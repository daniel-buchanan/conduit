# Roadmap

Gaps identified against MediatR (full detail and citations in [docs/research/mediatr-feature-gap.md](docs/research/mediatr-feature-gap.md)), with status and priority as decided 2026-09-19.

Policy: an item stays listed here until implemented. Once built, remove its entry — this file tracks what's outstanding, not a changelog.

## Priority order

### 1. Void-response requests + `IBaseRequest`/covariance

MediatR has a fire-and-forget request shape (`IRequest`/`IRequestHandler<TRequest>`, no response type), unified with typed requests under a common non-generic `IBaseRequest`, with `IRequest<out TResponse>` covariance. Conduit's `IRequest<TResult> where TResult : class` (`src/conduit/IRequest.cs:7`) requires every request to produce a `class` response, has no common non-generic base, and no variance.

**Gap**: no way to model a request with no meaningful return value without a fake response type. Adding a common base is also the prerequisite for a void-response type to share dispatch machinery with typed requests.

**Status**: bundled as one roadmap item — the two changes are linked (the common base is needed to support a void-response type cleanly).

### 2. Streaming requests

MediatR's `IStreamRequest<TResponse>`/`IStreamRequestHandler` returning `IAsyncEnumerable<TResponse>`. Zero references anywhere in Conduit's `src/`.

**Status**: not in current scope. Not previously documented as a gap anywhere (unlike Notification). On roadmap now.

### 3. Pipeline behaviors (onion-style middleware)

MediatR's `IPipelineBehavior<TRequest,TResponse>` runs code both before and after the handler, and can short-circuit without throwing. Conduit's `Stage` model is flat and sequential (`BuildablePipe.PushInternalAsync`, `src/conduit/Pipes/BuildablePipe.cs:49-58`); the only short-circuit is a thrown exception (`Pipe.HandleUnsuccessfulResult`, `src/conduit/Pipes/Pipe.cs:91-101`).

**Status**: intentional per [ADR-0002](docs/adr/0002-flat-sequential-stage-execution.md), which rejected a specific half-working `next`-delegate implementation — not a permanent rejection of wrap-before-and-after behavior in general. ADR-0002 itself notes a short-circuit mechanism should be designed explicitly if needed later. On roadmap, build only if a concrete need arises (e.g. transaction wrapping, logging spans, result-inspecting authorization).

### 4. Notification / pub-sub (`INotification`/`Publish`)

MediatR's one-event-to-many-handlers dispatch, pluggable publish strategy (sequential vs. concurrent). Conduit enforces strict 1:1 `Pipe` registration (`PipeAlreadyRegisteredException`); no multi-handler dispatch path exists.

**Status**: already tracked as a known gap in `CONTEXT.md`'s "Notification" glossary entry. Not currently required by any consumer. Lowest priority — keep on roadmap, remove if built.

**Follow-on**: once Notification exists, split `IConduit` into a `Push`-only and a `Publish`-only interface (mirroring MediatR's `ISender`/`IPublisher`), so consumers can depend on just the one they need. Not independently prioritized — tied to this item.

### 5. Runtime-typed `Push`

MediatR's `Send(object request)` returns `Task<object?>`, so a request whose type is only known at runtime (deserialized from a queue, routed by a generic endpoint) can be sent without reflection. `IConduit` only has `PushAsync<TResponse>(IRequest<TResponse>, ...)` (`src/conduit/IConduit.cs`); the caller must know `TResponse` at compile time or close the method by reflection.

**Status**: added 2026-11-26 from a re-check against MediatR. Medium weight; build when a consumer needs it.

### 6. Value-type responses

MediatR's `TResponse` is unconstrained. Conduit's `IRequest<TResult> where TResult : class` (`src/conduit/IRequest.cs`) rejects `int`, `bool`, `Guid` and similar, so those need a wrapper class. The `class` constraint repeats through `IConduit`, `IPipe`, `IRequestHandler` and the stage types, so removing it is a breaking change across the public surface.

**Status**: added 2026-11-26. Medium weight. Consider doing it together with item 1, which also reshapes `IRequest`.

### 7. Constrained default stages

MediatR runs an open behavior declared `where TRequest : ICommand` only for requests that satisfy the constraint. Conduit's `AddDefaultPreExecutionStage`/`AddDefaultPostExecutionStage` apply a stage to every pipe, and `StageMaterializer.Materialize` (`src/conduit/Helpers/StageMaterializer.cs`) calls `MakeGenericType` without checking constraints — so, by inspection, a constrained default stage throws `ArgumentException` when a non-matching pipe is built.

**Status**: added 2026-11-26 from the MediatR docs check ([docs/research/mediatr-feature-gap-docs-check.md](docs/research/mediatr-feature-gap-docs-check.md)). Medium weight. Confirm the throw with a failing test first; the likely fix is to skip a default stage whose constraints the request doesn't satisfy.

### 8. Exception action ordering default

MediatR's default runs exception actions only when no exception handler handled the exception (`ApplyForUnhandledExceptions`); running them for every exception is opt-in. Conduit's `ExceptionHandlerRunner` always runs actions first, with no switch (`src/conduit/Pipes/ExceptionHandlerRunner.cs`), so code ported from MediatR fires actions (alerts, error logs) for exceptions a handler recovered from.

**Status**: added 2026-11-26. Medium weight. A design decision, not a bug: choose the default (and whether to offer a switch) and record it in an ADR amending ADR-0016.

## Not gaps (parity already exists)

- **Pre/post-processors** — Conduit's `Stage` model natively supports pre/post-handler placement (`AddDefaultPreExecutionStage`/`AddDefaultPostExecutionStage`), covering MediatR's `IRequestPreProcessor`/`IRequestPostProcessor` use case.
- **Assembly scanning / auto-registration** — `RegisterHandlersFromAssemblyContaining`, `RegisterPipesFromAssemblyContaining`, `AddValidation()`'s app-domain scan already provide this.
- **Generic handler registration** — lower priority, not on this roadmap: the open-generic mechanism already works for framework stages (`ValidationStage<,>`); exposing it for user-authored handlers is a hardening task, not new capability.
