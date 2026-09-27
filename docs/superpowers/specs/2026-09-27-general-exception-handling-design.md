# General exception handling — design spec

Date: 2026-09-27
Roadmap item: [ROADMAP.md](../../../ROADMAP.md) #1, "General exception handling (real gap)"

## Problem

Conduit's only exception-handling mechanism today is `ConduitValidationExceptionHandler`
(`src/conduit.aspnetcore.validation/ConduitValidationExceptionHandler.cs`) — ASP.NET Core
`RequestDelegate` middleware, hardcoded to `ValidationFailedException`/`StageFailedException`/
`ValidatorNotFoundException`, living in the validation-integration package. Non-HTTP consumers
(worker services, console apps, queue handlers) get no structured exception handling from Conduit
at all, and there is no per-`(TRequest, TException)` interception point analogous to MediatR's
`IRequestExceptionHandler`/`IRequestExceptionAction` (see
[docs/research/mediatr-feature-gap.md](../../research/mediatr-feature-gap.md), item 5).

## Goals

- A general, per-`(TRequest, TException)` exception interception point that lives in `conduit`
  core — no ASP.NET Core dependency, works for any host.
- Two independent mechanisms, scoped separately:
  - **Request-scoped**: exceptions thrown by the `Handler` stage specifically. Named after
    MediatR's `IRequestExceptionHandler`/`IRequestExceptionAction` for direct cross-reference.
  - **Stage-scoped**: exceptions thrown by any other stage in the pipe (pre-execution,
    post-execution, including framework stages like `ValidationStage<,>`).
- Both support an "action" flavor (observe/log/react, cannot swallow — exception always
  propagates after) and a "handler" flavor (can swallow the exception and supply a response).
- Multiple handlers/actions may be registered for the same `(TRequest, [TResponse,] TException)`;
  each declares its own required `Order` to control sequencing.
- Matching walks the exception's real type hierarchy (the original exception the stage/handler
  threw), not `StageFailedException`'s wrapped form.
- Exceptions already marked `IPassthroughException` (`ValidationFailedException`,
  `StageFailedException`, `ValidatorNotFoundException`) are exempt — they keep propagating
  exactly as today, untouched by either new mechanism.

## Non-goals

- No change to `ConduitValidationExceptionHandler` or any ASP.NET Core behavior. It keeps working
  on whatever exception type ultimately escapes `PushAsync` (unhandled `StageFailedException`,
  `ValidationFailedException`, or now also anything a registered handler chose not to handle).
- No change to `IPipeStage` or the *stage-controlled* short-circuit model ADR-0002 rejected — a
  stage still cannot decide for itself whether the next one runs. `BuildablePipe`'s loop does gain
  a small pipe-level early-return check (see Dispatch algorithm), driven entirely by `ExecuteStage`'s
  outcome, never by a stage — this is exactly the "explicit mechanism on top of the flat model"
  ADR-0002 said would be needed later, not a reversal of it.
- No onion-style "wrap and call next" behavior (`IPipelineBehavior`). Out of scope, tracked
  separately as roadmap #4.
- Assembly-scan registration (`RegisterExceptionHandlersAsImplementedFrom<TLocator>()`) is
  in-scope but separable — see "Phasing" below.

## Components

New file(s) under `src/conduit/Exceptions/Handling/`:

```csharp
namespace conduit.Exceptions.Handling;

public sealed class RequestExceptionHandlerState<TResponse> where TResponse : class
{
    public bool Handled { get; private set; }
    public TResponse? Response { get; private set; }

    public void SetHandled(TResponse response)
    {
        Response = response;
        Handled = true;
    }
}

public interface IRequestExceptionHandler<TRequest, TResponse, TException>
    where TRequest : class, IRequest<TResponse>
    where TResponse : class
    where TException : Exception
{
    int Order { get; }
    Task HandleAsync(TRequest request, TException exception, RequestExceptionHandlerState<TResponse> state, CancellationToken cancellationToken);
}

public interface IRequestExceptionAction<TRequest, TException>
    where TRequest : class
    where TException : Exception
{
    int Order { get; }
    Task ExecuteAsync(TRequest request, TException exception, CancellationToken cancellationToken);
}

public interface IStageExceptionHandler<TRequest, TResponse, TException>
    where TRequest : class, IRequest<TResponse>
    where TResponse : class
    where TException : Exception
{
    int Order { get; }
    Task HandleAsync(TRequest request, TException exception, RequestExceptionHandlerState<TResponse> state, CancellationToken cancellationToken);
}

public interface IStageExceptionAction<TRequest, TException>
    where TRequest : class
    where TException : Exception
{
    int Order { get; }
    Task ExecuteAsync(TRequest request, TException exception, CancellationToken cancellationToken);
}
```

`Order` is a required `{ get; }` property, **not** a C# 8 default interface member. Grepping
`src/conduit*` found zero existing default-interface-implementation precedent, and `conduit` core
multi-targets `netstandard2.0;net10.0` specifically so consumers aren't forced onto the newest
runtime (ADR-0015) — default interface members are a *runtime* dispatch feature, unsupported on
.NET Framework, so a netstandard2.0 type implementing one of these interfaces without overriding a
DIM-based `Order` would throw `TypeLoadException` at load time on that runtime. Implementers just
write `public int Order => 0;` themselves as an ordinary member on their own concrete type — safe,
since that's normal virtual dispatch, not DIM.

No variance annotation on `TException`: resolution never asks the DI container to satisfy a
lookup via variance. The dispatch routine itself walks `e.GetType()`'s ancestors and issues one
exact closed-generic lookup per ancestor (see Dispatch below), so a handler registered for a base
type like `Exception` is found by that walk reaching `typeof(Exception)`, not by contravariant
container resolution.

`DebugResult<TResponse>` (`src/conduit/Pipes/DebugResult.cs`) gains a `bool ShortCircuited`
property, `true` when a handler's `SetHandled` caused the pipe to return early (see Dispatch step
8), `false` otherwise. `PushAsync` (the non-debug path) has no metadata channel and stays
unchanged — it only ever returned a bare `TResponse?`, and that limit predates this feature.

## Dispatch algorithm

Both mechanisms share one internal dispatch routine, parameterized by which interface pair to use
and which `IServiceProvider` to resolve against (the `Pipe<TRequest,TResponse>`'s existing
`provider` field). Invoked from `Pipe<TRequest,TResponse>.ExecuteStage`'s catch block
(`src/conduit/Pipes/Pipe.cs:74-82`), replacing the unconditional wrap-and-throw for exceptions that
get handled:

1. `catch (Exception e)` — as today.
2. If `e is IPassthroughException`, `throw;` immediately — unchanged, no dispatch.
3. Determine the interface pair: `stage is IRequestHandler` → Request-scoped
   (`IRequestExceptionHandler<,,>` / `IRequestExceptionAction<,>`); otherwise → Stage-scoped
   (`IStageExceptionHandler<,,>` / `IStageExceptionAction<,>`).
4. Walk `e.GetType()` up its inheritance chain up to and including `typeof(Exception)`. At each
   level, resolve `provider.GetServices(closedActionType)` and
   `provider.GetServices(closedHandlerType)` for that ancestor type, closing the generic over
   `(TRequest, TResponse, ancestorType)` (handlers) or `(TRequest, ancestorType)` (actions).
5. Concatenate all matches found across every hierarchy level (most-derived level enumerated
   first) into one list per flavor, then **stable-sort by `Order` ascending**. Stability preserves
   hierarchy-then-registration order as the tiebreak for equal `Order`.
6. Run every matching **action** (for this stage's `IRequestExceptionAction`/`IStageExceptionAction`
   set), in sorted order, unconditionally — always run in full, regardless of what the handler
   pass below does, and cannot suppress the eventual throw themselves.
7. Run matching **handlers**, in sorted order, until one calls `state.SetHandled(...)`. Stop there
   — do not run remaining handlers.
8. If `state.Handled`: **short-circuit the whole pipe**, uniformly regardless of whether it was the
   `Handler` or another stage that threw — build this stage's `StageMetric` (`Success: true`,
   `Exception: e`, timer stopped the same point the success path does), stop `stageTimer`, and
   signal `BuildablePipe.PushInternalAsync`'s loop to break immediately rather than continue to the
   next stage. No later stage runs — not the `Handler` if a pre-stage was handled, not any
   post-execution stage if the `Handler` itself was handled. This means `ExecuteStage`'s return
   contract needs a way to carry "stop here" back to the loop (e.g. a third tuple element), and
   `PushInternalAsync` must (a) `break` instead of continuing, (b) return a `Metrics` array
   truncated to the stages that actually ran (not the full pre-allocated
   `new StageMetric[stages.Length]` — see Testing plan), and (c) set
   `DebugResult.ShortCircuited = true`. A consumer who wants "always run regardless of
   short-circuit" (e.g. audit logging) uses the Action flavor, which already ran in step 6 before
   any of this.
9. If not handled by any handler (including zero handlers registered): fall through to today's
   behavior unchanged — wrap `e` in `StageFailedException` via `HandleUnsuccessfulResult` and
   throw. `DebugResult.ShortCircuited` stays `false` (moot in this branch anyway, since the pipe
   throws rather than returning a `DebugResult` at all).
10. If a resolved action or handler itself throws, that exception propagates directly — it is not
    caught, wrapped, or retried by the dispatch routine.

## Registration

No new Conduit-specific registration API is required for the minimum: these are ordinary DI
service types, registered the normal way:

```csharp
services.AddTransient<IRequestExceptionHandler<CreateOrder, OrderResult, SqlException>, RetryOnDeadlock>();
```

### Phasing

- **Phase 1 (this feature)**: the four interfaces, `RequestExceptionHandlerState<TResponse>`, and
  the dispatch change in `Pipe.ExecuteStage`. Manual DI registration only.
- **Phase 2 (separable, same PR unless it grows the diff significantly)**:
  `RegisterExceptionHandlersAsImplementedFrom<TLocator>()` on `IConduitConfigurationBuilder`,
  mirroring `RegisterHandlersAsImplementedFrom` (`src/conduit/Configuration/CondiutConfigurationBuilder.cs`),
  scanning for closed implementations of all four interfaces via `ReflectionHelper`.

## Error handling / edge cases

- **Zero handlers registered for a type**: identical to today's behavior — wrap and throw
  `StageFailedException`.
- **Exact same `(TRequest, [TResponse,] TException)` registered twice with the same `Order`**:
  allowed (per your answer: "allow multiple, add order property so they set their own order") —
  no duplicate-registration exception (unlike `PipeAlreadyRegisteredException`'s 1:1 pipe
  enforcement, which is a different concept). Both run; for handlers, only the first (by sort
  order, DI registration order as tiebreak) that calls `SetHandled` wins.
- **A handler resolved for an ancestor exception type also matches a more-derived registration**:
  both are collected (once each, at the hierarchy level they were registered against) and
  participate in the same sort — a handler registered for `Exception` runs in the same ordered
  list as one registered for the exact thrown type, ordered by `Order`, not by specificity.
- **`IPassthroughException` types**: fully exempt, per your answer — never enter dispatch, always
  `throw;` immediately, identical to current behavior.
- **Short-circuit is uniform**: a handled exception always stops the whole pipe immediately, no
  matter which stage threw — no special-casing "Handler exceptions still let post-stages run."
  This is a deliberate, single rule rather than two different behaviors depending on stage
  position; see Dispatch step 8.
- **`DebugResult.Metrics` on short-circuit**: truncated to exactly the stages that ran (never the
  full pre-allocated array with trailing `null`s) — a consumer reading `Metrics.Length` gets an
  honest count of what executed, and never needs to null-check an element the pipe never touched.
- **`RegisterPipe`-built pipes (not `RegisterHandler`)**: dispatch lives in `Pipe.ExecuteStage`,
  shared by both registration paths — no special-casing needed, both get this for free.
- **`stage is IRequestHandler` check for custom `IPipe` implementations that don't use
  `BuildablePipe`**: not applicable — dispatch is `Pipe<TRequest,TResponse>`-internal to
  `ExecuteStage`, only invoked by pipes that call the shared `ExecuteStage` helper (which is what
  `BuildablePipe` does). A fully custom `IPipe` that bypasses `Pipe<TRequest,TResponse>` entirely
  gets no dispatch, same as it gets none of `Pipe`'s other machinery today.

## Testing plan

New tests under `conduit.tests/Stages/` (or a new `conduit.tests/ExceptionHandling/` subfolder,
matching "put a new test in the folder matching what it covers"):

- Request-scoped action runs, exception still propagates (wrapped as `StageFailedException`).
- Request-scoped handler sets response → pipe short-circuits: returns that response immediately,
  no exception, and no post-execution stage runs. `DebugResult.ShortCircuited` is `true`,
  `Metrics` is truncated to the stages that ran (includes the `Handler`'s own metric, excludes
  every post-stage).
- Request-scoped handler that does *not* call `SetHandled` → falls through to wrap-and-throw.
- Stage-scoped equivalents of the above three, using a non-Handler stage — additionally: a
  *pre*-execution stage's handled exception must short-circuit before the `Handler` itself ever
  runs (confirms the auth-check-style scenario from the design discussion actually behaves as
  intended, not just the Handler-side case).
- Multiple handlers, `Order` respected — lower `Order` runs first; first to call `SetHandled`
  wins, later ones don't run.
- Multiple actions, `Order` respected, all run regardless of any handler's outcome, and regardless
  of short-circuit (actions always run in full before any handler is even attempted).
- Hierarchy walk: handler registered for a base exception type catches a derived exception thrown
  by the stage.
- `IPassthroughException` types (`ValidationFailedException`, `StageFailedException`,
  `ValidatorNotFoundException`) bypass dispatch entirely even when a matching handler is
  registered for them — confirm existing passthrough behavior is unchanged.
- No handler registered at all — existing `StageFailedException`-wrap behavior unchanged
  (regression coverage for current behavior).
- Handler/action that itself throws — exception propagates directly, not wrapped.
- `DebugResult.ShortCircuited` is `false` on every pipe run that never hits a handled exception
  (regression coverage — the common case must not regress to `true`).
- `PushWithDebugAsync`'s `Metrics` array has no `null` elements after a short-circuited run (guards
  against the pre-allocation hole described in the Dispatch algorithm).

`dotnet build` / `dotnet test` per [CLAUDE.md](../../../CLAUDE.md) gate; `conduit` targets
`netstandard2.0;net10.0` so this must build clean on both TFMs (no `net10.0`-only APIs in the new
interfaces/dispatch code).

## Documentation follow-up

- `CONTEXT.md`: add glossary entries for the four new interfaces (or one combined entry) once
  named/placed, following existing entry shape (definition + `_Avoid_` where relevant, e.g. avoid
  "middleware"/"pipeline behavior" as synonyms — these are exception hooks, not onion middleware).
- `ROADMAP.md`: remove item #1 in the same commit that lands this feature, per its own policy.
- ADR: see companion ADR-0016 (this decision resolves the "design an explicit mechanism later"
  note left open by ADR-0002, and extends ADR-0004's `IPassthroughException` exemption list
  semantics to a second consumer).
