# Roadmap

Gaps identified against MediatR (full detail and citations in [docs/research/mediatr-feature-gap.md](docs/research/mediatr-feature-gap.md)), with status and priority as decided 2026-09-19.

Policy: an item stays listed here until implemented. Once built, remove its entry — this file tracks what's outstanding, not a changelog.

## Priority order

### 1. General exception handling (real gap)

Conduit's only exception-handling mechanism is `ConduitValidationExceptionHandler` (`src/conduit.validation/ConduitValidationExceptionHandler.cs`) — ASP.NET Core `RequestDelegate` middleware, hardcoded to `ValidationFailedException`/`StageFailedException`, validation-package-scoped.

**Gap**: no general per-`(TRequest, TException)` interception point (MediatR's `IRequestExceptionHandler`/`IRequestExceptionAction`). Non-HTTP consumers (worker services, console apps, queue handlers) get no structured exception handling from Conduit at all.

**Status**: confirmed real limitation, not an intentional design choice. Highest priority.

### 2. Void-response requests + `IBaseRequest`/covariance

MediatR has a fire-and-forget request shape (`IRequest`/`IRequestHandler<TRequest>`, no response type), unified with typed requests under a common non-generic `IBaseRequest`, with `IRequest<out TResponse>` covariance. Conduit's `IRequest<TResult> where TResult : class` (`src/conduit/IRequest.cs:7`) requires every request to produce a `class` response, has no common non-generic base, and no variance.

**Gap**: no way to model a request with no meaningful return value without a fake response type. Adding a common base is also the prerequisite for a void-response type to share dispatch machinery with typed requests.

**Status**: bundled as one roadmap item — the two changes are linked (the common base is needed to support a void-response type cleanly).

### 3. Streaming requests

MediatR's `IStreamRequest<TResponse>`/`IStreamRequestHandler` returning `IAsyncEnumerable<TResponse>`. Zero references anywhere in Conduit's `src/`.

**Status**: not in current scope. Not previously documented as a gap anywhere (unlike Notification). On roadmap now.

### 4. Pipeline behaviors (onion-style middleware)

MediatR's `IPipelineBehavior<TRequest,TResponse>` runs code both before and after the handler, and can short-circuit without throwing. Conduit's `Stage` model is flat and sequential (`BuildablePipe.PushInternalAsync`, `src/conduit/Pipes/BuildablePipe.cs:48-52`); the only short-circuit is a thrown exception (`Pipe.HandleUnsuccessfulResult`, `src/conduit/Pipes/Pipe.cs:79-88`).

**Status**: intentional per [ADR-0002](docs/adr/0002-flat-sequential-stage-execution.md), which rejected a specific half-working `next`-delegate implementation — not a permanent rejection of wrap-before-and-after behavior in general. ADR-0002 itself notes a short-circuit mechanism should be designed explicitly if needed later. On roadmap, build only if a concrete need arises (e.g. transaction wrapping, logging spans, result-inspecting authorization).

### 5. Notification / pub-sub (`INotification`/`Publish`)

MediatR's one-event-to-many-handlers dispatch, pluggable publish strategy (sequential vs. concurrent). Conduit enforces strict 1:1 `Pipe` registration (`PipeAlreadyRegisteredException`); no multi-handler dispatch path exists.

**Status**: already tracked as a known gap in `CONTEXT.md`'s "Notification" glossary entry. Not currently required by any consumer. Lowest priority — keep on roadmap, remove if built.

**Follow-on**: once Notification exists, split `IConduit` into a `Push`-only and a `Publish`-only interface (mirroring MediatR's `ISender`/`IPublisher`), so consumers can depend on just the one they need. Not independently prioritized — tied to this item.

## Not gaps (parity already exists)

- **Pre/post-processors** — Conduit's `Stage` model natively supports pre/post-handler placement (`AddDefaultPreExecutionStage`/`AddDefaultPostExecutionStage`), covering MediatR's `IRequestPreProcessor`/`IRequestPostProcessor` use case.
- **Assembly scanning / auto-registration** — `RegisterHandlersAsImplementedFrom`, `RegisterPipesAsImplementedFrom`, `AddValidation()`'s app-domain scan already provide this.
- **Generic handler registration** — lower priority, not on this roadmap: the open-generic mechanism already works for framework stages (`ValidationStage<,>`); exposing it for user-authored handlers is a hardening task, not new capability.
