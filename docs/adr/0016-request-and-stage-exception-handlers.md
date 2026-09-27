# Structured exception handling is two typed interception points at the Pipe level, not a Stage

Conduit's only exception-handling mechanism was `ConduitValidationExceptionHandler`, ASP.NET Core
middleware hardcoded to three known exception types and scoped to `conduit.aspnetcore.validation`.
Non-HTTP consumers (worker services, console apps, queue handlers) got no structured exception
handling from Conduit at all, and there was no per-`(TRequest, TException)` interception point
analogous to MediatR's `IRequestExceptionHandler`/`IRequestExceptionAction`. ADR-0002 flagged this
class of gap directly: "if per-stage short-circuiting is needed later, it should be designed as an
explicit mechanism on top of [the] flat model, not revived as a half-implemented `next` delegate."

We add four DI-resolvable interfaces in `conduit` core (no ASP.NET Core or validation-package
dependency): `IRequestExceptionHandler<TRequest,TResponse,TException>` /
`IRequestExceptionAction<TRequest,TException>`, scoped to exceptions thrown by the `Handler` stage
specifically (named to match MediatR's equivalents directly), and
`IStageExceptionHandler<TRequest,TResponse,TException>` / `IStageExceptionAction<TRequest,TException>`,
scoped to exceptions thrown by any other stage (pre-execution, post-execution, including
framework stages such as `ValidationStage<,>`). "Handler" and "everything else" are two separate
mechanisms rather than one, because a Handler-stage failure is a business-logic exception a
consumer wants to reason about per-request-type (MediatR's exact use case), while a non-Handler
stage failure is a cross-cutting pipeline concern with different scoping needs — collapsing them
into one interface would force every consumer to distinguish "which stage" inside the handler body
instead of at the registration/type level.

Both mechanism pairs are dispatched from `Pipe<TRequest,TResponse>.ExecuteStage`'s existing catch
block, discriminated by the pre-existing `IRequestHandler` marker interface (`stage is
IRequestHandler`) — no new marker was needed. This keeps the change entirely inside `Pipe`: no
change to `IPipeStage`, or to a stage's own ability (still none) to decide whether the next stage
runs. An "action" flavor observes/reacts, always runs in full, and cannot suppress the eventual
throw; a "handler" flavor can call `RequestExceptionHandlerState<TResponse>.SetHandled(response)`
to supply a response — and doing so **short-circuits the whole pipe**, uniformly regardless of
which stage threw: `BuildablePipe`'s loop stops immediately rather than continuing to whatever
stage comes next. This is a deliberate reversal of the "later stages may override the response"
read of ADR-0005 for this one case — ADR-0005 governs stages that complete normally and
legitimately produce a later response; a *handled failure* is different; letting the loop continue
after it (e.g. running the real `Handler` after a pre-stage auth failure was "handled") would
silently undo the handling. This is exactly the "explicit mechanism on top of the flat model"
ADR-0002 anticipated needing later: `ExecuteStage`'s return contract gains a way to signal "stop
here," and `PushInternalAsync` breaks out of its loop on that signal rather than running the
remaining stages — the model stays flat and stage-controlled short-circuiting is still rejected;
only the `Pipe` itself, driven by this feature, can now stop early. `DebugResult<TResponse>` gains
`bool ShortCircuited` (true exactly when this happened) and its `Metrics` array is truncated to
the stages that actually ran, rather than left the original stage-count length with trailing
`null`s. Multiple handlers/actions may be registered for the same
`(TRequest, [TResponse,] TException)`; each carries a *required* `Order` property (deliberately not
a C# 8 default interface member — `conduit` core multi-targets `netstandard2.0;net10.0`
specifically so consumers aren't forced onto the newest runtime, per ADR-0015, and default
interface members are unsupported at the dispatch level on .NET Framework; a required member has
no such risk), and matching walks the exception's real inheritance chain rather than requiring an
exact type match.

Exception types already marked `IPassthroughException` (`ValidationFailedException`,
`StageFailedException`, `ValidatorNotFoundException` — see ADR-0004) are exempt from both new
mechanisms and continue to propagate unwrapped exactly as before dispatch was added; `ADR-0004`'s
exemption list now has a second consumer (the new dispatch routine) alongside `ExecuteStage`'s
own wrap-vs-passthrough check, but the list itself, and the reasoning behind it (these are already
Conduit's own stable, expected-failure types), is unchanged.

We rejected modeling this as a new kind of `Stage` (e.g. `IExceptionHandlingStage` occupying a
position in the stage list): exception handling applies pipeline-wide per-request-type, not to one
position, and the Handler/other-stage split has no natural single slot to hold both. We also
rejected extending `ConduitValidationExceptionHandler`'s hardcoded array to be user-extensible —
that mechanism is HTTP-only by construction, which is the exact shape of the gap this ADR closes,
not a viable fix for it.
