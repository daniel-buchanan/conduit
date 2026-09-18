# Feature gap: Conduit vs. MediatR

## Summary

Conduit implements the strict 1:1 request/response half of MediatR's feature surface — a `Push` call routes an `IRequest<TResult>` to exactly one `IRequestHandler` through a `Pipe` of `Stage`s, resolved via DI, with optional assembly-scanning registration and a validation stage that plugs in as a `Stage`. That much has a real (if differently named) equivalent to MediatR's `IRequest<TResponse>`/`IRequestHandler`/`Send`/`RegisterServicesFromAssembly`.

Everything that depends on MediatR's **onion-style pipeline** (`IPipelineBehavior<,>`, which can run code both before *and* after the handler and can short-circuit without throwing) and everything built on **pub/sub notifications** (`INotification`/`Publish`, one event to many handlers, pluggable publish strategies) is absent from Conduit today. The `Pipe`/`Stage` model is architecturally flat and sequential by deliberate design (ADR-0002), not an oversight — it cannot express "wrap the handler," only "run before it or after it as a separate stage." Notifications are an explicitly-acknowledged, not-yet-designed gap per `CONTEXT.md`. MediatR's dedicated pre/post-processor and exception-handler/action interfaces have no Conduit analogue as typed abstractions; Conduit's only exception-handling surface is a single ASP.NET Core HTTP middleware (`ConduitValidationExceptionHandler`) that is validation-specific in practice, not a general per-request-type interception point. Streaming requests (`IStreamRequest<TResponse>`/`IAsyncEnumerable`) do not exist in Conduit at all.

Sources consulted (primary): MediatR README (`github.com/jbogard/MediatR` → redirects to `github.com/LuckyPennySoftware/MediatR`, `main` branch) and the actual interface/registration source under `src/MediatR/` in that repository, fetched directly from `raw.githubusercontent.com`. Conduit facts are drawn from `origin/feature/validation` in this repository (this worktree's checkout is behind that branch, so files were read via `git show origin/feature/validation:<path>` rather than the working tree).

---

## Feature-by-feature

### 1. `IRequest<TResponse>` / `IRequestHandler` — baseline request/response

**MediatR**: `IRequest<out TResponse> : IBaseRequest` (covariant, unconstrained `TResponse`) and a separate void-response `IRequest : IBaseRequest`, both closed by `IRequestHandler<TRequest,TResponse>`/`IRequestHandler<TRequest>`. Dispatched via `ISender.Send`/`IMediator.Send`.
Source: `https://raw.githubusercontent.com/LuckyPennySoftware/MediatR/main/src/MediatR.Contracts/IRequest.cs`, `https://raw.githubusercontent.com/LuckyPennySoftware/MediatR/main/src/MediatR/IRequestHandler.cs`, `https://raw.githubusercontent.com/LuckyPennySoftware/MediatR/main/src/MediatR/ISender.cs`.

**Conduit status**: Full equivalent (different names, narrower generics).
- `IRequest<TResult> where TResult : class` — `src/conduit/IRequest.cs:7`. No `IBaseRequest`-style non-generic parent, no covariance (`out`), and `TResult` is constrained to `class` (MediatR allows value-type responses).
- `IRequestHandler<TRequest,TResponse> : IPipeStage<TRequest,TResponse>, IRequestHandler` with `HandleAsync` — `src/conduit/IRequestHandler.cs:16-27`.
- Dispatch: `IConduit.PushAsync<TResponse>(IRequest<TResponse>, CancellationToken)` — `src/conduit/IConduit.cs`, implemented in `src/conduit/Conduit.cs` (resolves `IPipe<,>` from DI, throws `PipeNotFoundException`).
- No void-response request type (`IRequest` with no type parameter) — every Conduit request must declare a reference-type response.

**Notes**: This is Conduit's core, well-covered surface — ADR-0001 documents the deliberate `Push`/`Send` naming departure only; the request/response contract itself is intended to match MediatR's.

### 2. `INotification`/`INotificationHandler`/`Publish` — pub/sub

**MediatR**: `INotification` marker + `INotificationHandler<TNotification>.Handle`, dispatched via `IPublisher.Publish`/`IMediator.Publish` to *all* registered handlers for that notification type. Publish strategy is pluggable via `INotificationPublisher`: default is `ForeachAwaitPublisher` (awaits each handler in turn, in registration order, inside a single `foreach`), with `TaskWhenAllPublisher` (fires all handler tasks and `Task.WhenAll`s them, so handlers run concurrently) shipped as an alternative, settable via `MediatRServiceConfiguration.NotificationPublisher`/`NotificationPublisherType`.
Sources: `https://raw.githubusercontent.com/LuckyPennySoftware/MediatR/main/src/MediatR.Contracts/INotification.cs`, `.../src/MediatR/INotificationHandler.cs`, `.../src/MediatR/INotificationPublisher.cs`, `.../src/MediatR/NotificationPublishers/ForeachAwaitPublisher.cs`, `.../src/MediatR/NotificationPublishers/TaskWhenAllPublisher.cs`, `.../src/MediatR/MicrosoftExtensionsDI/MediatrServiceConfiguration.cs` (`NotificationPublisher` property, default `new ForeachAwaitPublisher()`).

**Conduit status**: None. Confirmed by source inspection, not just `CONTEXT.md`'s own claim: there is no `Push`/pipe concept that resolves more than one handler per request type — `Conduit.PushInternalAsync` resolves a single `IPipe<TRequest,TResponse>` from DI (`src/conduit/Conduit.cs`), and `PipeAlreadyRegisteredException` (`src/conduit/Exceptions/PipeAlreadyRegisteredException.cs`) exists specifically to enforce one `Pipe` per `(TRequest,TResponse)` pair. `CONTEXT.md`'s glossary entry for "Notification" states this explicitly: "Not yet implemented... Known gap, not yet designed — when it lands, its vocabulary must not collide with `Pipe`/`Stage`."

**Notes**: Acknowledged, undesigned gap — not an ADR-backed intentional omission, just not built yet.

### 3. `IPipelineBehavior<TRequest,TResponse>` — onion-style middleware

**MediatR**: `Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)`. A behavior can run code before calling `next()`, inspect/alter the result after awaiting it, and can choose not to call `next()` at all (short-circuit) without throwing. Behaviors nest around the handler and each other (registration order = outer-to-inner). Registered via `cfg.AddBehavior<T>()` / `cfg.AddOpenBehavior(typeof(T<,>))`.
Source: `https://raw.githubusercontent.com/LuckyPennySoftware/MediatR/main/src/MediatR/IPipelineBehavior.cs`; registration API in `.../src/MediatR/MicrosoftExtensionsDI/MediatrServiceConfiguration.cs` (`AddBehavior`, `BehaviorsToRegister`); README section "To register behaviors..." at `https://raw.githubusercontent.com/jbogard/MediatR/master/README.md`.

**Conduit status**: None — architecturally excluded by design, per ADR-0002.
- `IPipeStage<TRequest,TResponse>.ExecuteAsync(Guid, TRequest, CancellationToken)` takes no `next` delegate — `src/conduit/Pipes/IPipeStage.cs`.
- `BuildablePipe<TRequest,TResponse>.PushInternalAsync` loops the stage-type array in a flat `for` loop, always calling every stage in order (`src/conduit/Pipes/BuildablePipe.cs:48-52`); there is no way for a stage to decide whether the next one runs.
- ADR-0002 (`docs/adr/0002-flat-sequential-stage-execution.md`) documents that a `next`-delegate overload existed previously, was never functionally wired up (the base implementation "ran itself, then unconditionally invoked `next` and discarded the result"), and was deliberately removed in favor of the current flat model.
- The closest Conduit gets to "short-circuit" is: a `Stage` returns an unsuccessful `StageResult` (e.g. failed validation), and `Pipe<TRequest,TResponse>.HandleUnsuccessfulResult` immediately **throws** (`ValidationFailedException` or `StageFailedException`) from inside the loop (`src/conduit/Pipes/Pipe.cs:79-88`), which does stop later stages from running — but only via an exception unwinding the call stack, not via a graceful "don't call next" the way an `IPipelineBehavior` can.

**Notes**: This is an **intentional** design departure (ADR-0002 exists specifically to record it), not an unaddressed gap — but it is the single biggest behavioral difference for anyone porting real MediatR usage, since `IPipelineBehavior` is the mechanism most non-trivial MediatR codebases lean on (logging spans, transactions, caching, retry, authorization checks that need to run only when the handler succeeds, etc. — all "before *and* after" patterns).

### 4. `IRequestPreProcessor<TRequest>` / `IRequestPostProcessor<TRequest,TResponse>`

**MediatR**: Dedicated `Process(TRequest, ct)` / `Process(TRequest, TResponse, ct)` interfaces, auto-registered when `AutoRegisterRequestProcessors` is set. Internally these are *not* a separate execution mechanism — `RequestPreProcessorBehavior<TRequest,TResponse>` and `RequestPostProcessorBehavior<TRequest,TResponse>` are themselves `IPipelineBehavior<,>` implementations that iterate the registered processors and then call/have called `next()`.
Source: `https://raw.githubusercontent.com/LuckyPennySoftware/MediatR/main/src/MediatR/Pipeline/IRequestPreProcessor.cs`, `.../IRequestPostProcessor.cs`, `.../RequestPreProcessorBehavior.cs`.

**Conduit status**: Partial equivalent, structurally different. Conduit's `Stage` concept already covers "run before/after the handler" as a first-class idea — `DefaultPipeConfiguration<TRequest,TResponse,THandler>.GetStages()` explicitly assembles `PreExecutionStages + [Handler] + PostExecutionStages` (`src/conduit/Configuration/DefaultPipeConfiguration.cs`), and `IConduitConfigurationBuilder` exposes `AddDefaultPreExecutionStage`/`AddDefaultPostExecutionStage` (`src/conduit/Configuration/CondiutConfigurationBuilder.cs`). `conduit.validation`'s `ValidationStage<,>` is registered as exactly such a default pre-execution stage (`src/conduit.validation/ConfigurationBuilderExtensions.cs`). So the *concept* MediatR splits into three things (pipeline behavior, pre-processor, post-processor) collapses in Conduit into one thing (`Stage`, ordered by registration position) — there's no dedicated "PreProcessor"/"PostProcessor" interface distinct from a general `IPipeStage`, but the pre/post placement itself is supported and is arguably simpler/more direct than MediatR's approach here.

**Notes**: Not a gap in capability for pure pre/post hooks — it's a deliberate simplification consistent with the flat-stage model (ADR-0002), just under different vocabulary.

### 5. `IRequestExceptionHandler<TRequest,TResponse,TException>` / `IRequestExceptionAction<TRequest,TException>`

**MediatR**: Typed, per-request/per-exception-type interception, implemented as `IPipelineBehavior` wrappers (`RequestExceptionProcessorBehavior`, `RequestExceptionActionProcessorBehavior`) around the *handler call itself* — they catch exceptions the handler throws and can set a response via `RequestExceptionHandlerState<TResponse>` (handler) or just observe/log/rethrow (action). Auto-discovered and registered as open generics during assembly scan.
Source: `https://raw.githubusercontent.com/LuckyPennySoftware/MediatR/main/src/MediatR/Pipeline/IRequestExceptionHandler.cs`, `.../IRequestExceptionAction.cs`; registration in `.../src/MediatR/Registration/ServiceRegistrar.cs` (`ConnectImplementationsToTypesClosing(typeof(IRequestExceptionHandler<,,>), ...)`).

**Conduit status**: None as a general, per-request-type mechanism — Conduit's actual exception handling is validation-specific and lives outside the Pipe entirely.
- `src/conduit.validation/ConduitValidationExceptionHandler.cs` is ASP.NET Core `RequestDelegate` middleware (constructor takes `RequestDelegate next`, `InvokeAsync(HttpContext)`), not a Conduit `Stage` or a typed per-`(TRequest,TException)` hook. It catches whatever bubbles out of the whole HTTP request pipeline (of which a `Push` call is just one part), pattern-matches on `ValidationFailedException`/`StageFailedException` via a hardcoded `KnownExceptionHandlers` array, and converts them to `ProblemDetails` HTTP responses.
- `src/conduit/Exceptions/StageFailedException.cs`, `ValidationFailedException.cs` are generic exception *types* thrown by the pipe engine (`Pipe.cs:79-88`), but there is no extensibility point for a consumer to register "when `MyHandler` throws `MyException`, do X" the way MediatR's exception handler/action interfaces allow — you'd have to add a case to `ConduitValidationExceptionHandler`'s hardcoded array or write your own ASP.NET Core middleware.
- The name `ConduitValidationExceptionHandler` and its location inside `conduit.validation` (not `conduit` core) both signal it was built for the validation use case specifically, not as a general Conduit exception-handling feature.

**Notes**: Real gap, and a meaningful one — MediatR's exception handler/action pair is a targeted per-type mechanism that composes with DI and assembly scanning; Conduit's only analogue is a single hand-maintained HTTP middleware.

### 6. Streaming requests (`IStreamRequest<TResponse>` / `IAsyncEnumerable`)

**MediatR**: `IStreamRequest<out TResponse>` / `IStreamRequestHandler<TRequest,TResponse>.Handle(...) : IAsyncEnumerable<TResponse>`, dispatched via `ISender.CreateStream`, with its own `IStreamPipelineBehavior<TRequest,TResponse>` (same onion-composition idea, but iterating rather than awaiting).
Source: `https://raw.githubusercontent.com/LuckyPennySoftware/MediatR/main/src/MediatR/IStreamRequestHandler.cs`, `.../IStreamPipelineBehavior.cs`, `.../ISender.cs` (`CreateStream` methods). Confirmed present in the current `main` branch source tree (`src/MediatR/IStreamPipelineBehavior.cs`, `IStreamRequestHandler.cs`, `Wrappers/StreamRequestHandlerWrapper.cs`).

**Conduit status**: None. No `IAsyncEnumerable`, `Stream`, or streaming-related type anywhere in `src/` (confirmed by searching the `feature/validation` source tree — zero matches for `IAsyncEnumerable`/`StreamRequest`/`IStream`). `IConduit` only exposes `PushAsync`/`PushWithDebugAsync`, both `Task<T>`-returning.

**Notes**: Unaddressed gap, not mentioned in `CONTEXT.md` or any ADR at all (unlike Notifications, which at least get an acknowledgment).

### 7. Auto-registration / assembly scanning

**MediatR**: `services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(...))` scans the given assembly/assemblies and auto-registers `IRequestHandler<,>`, `IRequestHandler<>`, `INotificationHandler<>`, `IStreamRequestHandler<,>`, `IRequestExceptionHandler<,,>`, `IRequestExceptionAction<,>`, and (opt-in via `AutoRegisterRequestProcessors`) pre/post processors — all via reflection in `ServiceRegistrar.AddMediatRClasses`. Behaviors themselves are *not* scanned automatically; they're added explicitly via `cfg.AddBehavior<T>()`/`cfg.AddOpenBehavior(...)`.
Source: README "Registering with `IServiceCollection`" section (`https://raw.githubusercontent.com/jbogard/MediatR/master/README.md`); `https://raw.githubusercontent.com/LuckyPennySoftware/MediatR/main/src/MediatR/Registration/ServiceRegistrar.cs`.

**Conduit status**: Partial/Full equivalent — Conduit does have assembly scanning, contrary to what one might assume from "manual per-pipe registration required":
- `IConduitConfigurationBuilder.RegisterHandlersAsImplementedFrom<TLocator>()` — scans `TLocator`'s assembly for `IRequestHandler` implementations and auto-registers the pipe/pre/post/handler descriptors for each (`src/conduit/IConduitConfigurationBuilder.cs:25`, implemented `src/conduit/Configuration/CondiutConfigurationBuilder.cs`, using `ReflectionHelper.GetTypesFromAssembly`/`GetServiceDescriptorsForHandler`, `src/conduit/Helpers/ReflectionHelper.cs`).
- `IConduitConfigurationBuilder.RegisterPipesAsImplementedFrom<TLocator>()` does the same for fully custom `IPipe` implementations (`src/conduit/IConduitConfigurationBuilder.cs:44`).
- `conduit.validation`'s `AddValidation()` parameterless overload scans **all currently loaded app-domain assemblies** for validators (`src/conduit.validation/ConfigurationBuilderExtensions.cs`, `AppDomain.CurrentDomain.GetAssemblies()` + `WithValidatorsFromAssembly`), which is broader (and riskier) than MediatR's explicit "you name the assemblies" approach.
- Caveat, per `docs/edge-case-review.md` Q7/Q13: the underlying `ReflectionHelper.GetRegistrationsAsImplementedFrom` fallback path (used by `RegisterPipesAsImplementedFrom`) is flagged as a real bug — "registers a concrete type under itself when generic args can't be resolved," producing an unreachable pipe instead of failing loudly — and is called out as unresolved/untested in that review.

**Notes**: Not a gap — Conduit was designed with assembly scanning from early on. The difference is granularity/robustness (MediatR's `ServiceRegistrar` has had years of hardening — timeout/limit guards for generic handler explosion, `MaxGenericTypeRegistrations`, etc. — Conduit's reflection path has a known-buggy fallback per the edge-case review) rather than presence/absence.

### 8. Generic handlers (open generic `IRequestHandler<,>` implementations)

**MediatR**: Explicitly supported and off by default — `MediatRServiceConfiguration.RegisterGenericHandlers` (default `false`) controls whether the assembly scan will register concrete types that still `ContainsGenericParameters` (i.e., open generic handler definitions like `class LoggingHandler<TReq,TResp> : IRequestHandler<TReq,TResp>`), guarded by `MaxGenericTypeParameters`/`MaxTypesClosing`/`MaxGenericTypeRegistrations`/`RegistrationTimeout` to bound the reflection blow-up.
Source: `https://raw.githubusercontent.com/LuckyPennySoftware/MediatR/main/src/MediatR/MicrosoftExtensionsDI/MediatrServiceConfiguration.cs`, `https://raw.githubusercontent.com/LuckyPennySoftware/MediatR/main/src/MediatR/Registration/ServiceRegistrar.cs`.

**Conduit status**: None for business-logic `IRequestHandler<,>` specifically — `RegisterHandlersAsImplementedFrom<TLocator>` reads `t.GetGenericArguments()` off each discovered concrete handler type expecting *closed* generic arguments (`src/conduit/Configuration/CondiutConfigurationBuilder.cs`); there's no `RegisterGenericHandlers`-style opt-in and no arity/registration-count safety valve. However, the underlying *mechanism* for open-generic **stages** (not business handlers) already exists and is used in production by `conduit.validation`: `ValidationStage<,>` is registered once as an open generic (`services.AddDescriptor(new ServiceDescriptor(typeof(ValidationStage<,>), typeof(ValidationStage<,>), ServiceLifetime.Transient))`, `src/conduit.validation/ConfigurationBuilderExtensions.cs`) and `DefaultPipeConfiguration<TRequest,TResponse,THandler>.MaterializeTypes`/`PipeFactory.MaterializeDefaultStages` close it per-request-type at pipe-build time (`src/conduit/Configuration/DefaultPipeConfiguration.cs`, `src/conduit/Pipes/PipeFactory.cs`).

**Notes**: Partial — the plumbing for "one open-generic type serves every request type" exists and is exercised for framework stages, but isn't exposed/hardened as a supported pattern for user-authored `IRequestHandler<,>` business logic the way MediatR's `RegisterGenericHandlers` is.

### 9. `IBaseRequest`/covariance, `Unit`, `ISender`/`IPublisher` split, licensing

- **`IBaseRequest` + covariance**: MediatR's `IRequest<out TResponse> : IBaseRequest` lets `IBaseRequest` serve as a common non-generic constraint across void and typed requests, and covariance lets an `IRequest<Derived>` be treated as `IRequest<Base>`. Source: `https://raw.githubusercontent.com/LuckyPennySoftware/MediatR/main/src/MediatR.Contracts/IRequest.cs`. Conduit's `IRequest<TResult> where TResult : class` (`src/conduit/IRequest.cs:7`) has neither a common non-generic base nor variance annotations, and there is no void-response `IRequest` at all — every Conduit request must produce a `class` response.
- **`Unit`**: still present in MediatR (`src/MediatR.Contracts/Unit.cs`, confirmed in the repo tree), historically MediatR's "no real response" placeholder type; current MediatR instead gives void requests their own `IRequestHandler<TRequest>`/`Task Handle(...)` overload (see item 1), so `Unit` is legacy/secondary rather than central. Conduit has no equivalent type or void-request path.
- **`ISender`/`IPublisher` split**: `IMediator : ISender, IPublisher` — `Send`/`CreateStream` live on `ISender`, `Publish` on `IPublisher`, so a consumer can depend on just the one it needs. Source: `https://raw.githubusercontent.com/LuckyPennySoftware/MediatR/main/src/MediatR/ISender.cs`, `.../IPublisher.cs`, `.../IMediator.cs`. Conduit has a single `IConduit` interface with `PushAsync`/`PushWithDebugAsync` (`src/conduit/IConduit.cs`) — no split, which is consistent with Conduit having no publish/notification side to split off in the first place.
- **Licensing**: current MediatR requires a license key (`MediatRServiceConfiguration.LicenseKey` / `Mediator.LicenseKey`, resolved from `MEDIATR_LICENSE_KEY`/`LUCKYPENNY_LICENSE_KEY` env vars if not set in code), per the README's "Setting the license key" / "Auto-discovery via environment variables" sections (`https://raw.githubusercontent.com/jbogard/MediatR/master/README.md`) and `Mediator`'s constructor calling `_serviceProvider.CheckLicense()` (`https://raw.githubusercontent.com/LuckyPennySoftware/MediatR/main/src/MediatR/Mediator.cs`). Conduit has no licensing mechanism of any kind in its source — a difference worth flagging for anyone evaluating Conduit specifically as a non-commercial alternative.

---

## Biggest gaps, ranked

Ranked by how load-bearing the missing feature would likely be for someone porting a real-world MediatR codebase to Conduit, validated against what actually exists in `src/conduit*` (not assumed):

1. **`IPipelineBehavior`-style onion middleware (item 3).** This is MediatR's single most-used extension point in production codebases — cross-cutting concerns (logging spans, transactions/unit-of-work, caching, retry/circuit-breaking, authorization that needs to inspect the *result*) all rely on "run code before *and* after, with the ability to short-circuit gracefully." Conduit's flat `Stage` loop (`BuildablePipe.cs:48-52`) cannot express "wrap"; it can only insert stages before/after, and its only short-circuit mechanism is throwing (`Pipe.cs:79-88`). This is the most consequential change for a porting exercise, and it is an **intentional, ADR-documented** design choice (ADR-0002), not an oversight — so the fix (if wanted) is a new mechanism layered on top of the flat model, not a bug fix.

2. **`INotification`/`Publish` pub-sub (item 2).** Any MediatR codebase using domain events (`Publish` to N handlers) has no path onto Conduit at all today — confirmed by the absence of any multi-handler dispatch path in `Conduit.cs`/`IConduit.cs` and by `PipeAlreadyRegisteredException` actively enforcing 1:1. This is the gap `CONTEXT.md` itself flags as "known, not yet designed," so it's already on the project's radar, but it remains a hard blocker for that class of usage.

3. **Structured exception handling (item 5).** MediatR's `IRequestExceptionHandler`/`IRequestExceptionAction` give per-`(request, exception)`-type interception with DI/assembly-scan support. Conduit's only mechanism is one hardcoded ASP.NET Core middleware scoped to validation (`ConduitValidationExceptionHandler.cs`), which (a) only fires for HTTP-hosted consumers, and (b) requires editing framework source to add a new case. Anyone with custom `IRequestExceptionHandler`/`IRequestExceptionAction` usage in MediatR has no equivalent extension point.

4. **Streaming requests (item 6).** Complete absence, and unlike notifications it isn't even acknowledged as a gap anywhere in the repo's own documentation. Likely lower real-world impact than #1–#3 (a smaller fraction of MediatR codebases use `IStreamRequest`), but worth flagging precisely because it's undocumented as a gap.

5. **Generic handler registration hardening (item 8).** Lower priority: the underlying open-generic-stage mechanism already works (`ValidationStage<,>`), so this is more "expose and harden an existing internal pattern for user handlers, with the same kind of safety limits MediatR added (`MaxGenericTypeParameters` etc.)" than "build a new capability."

Not counted as a gap: pre/post-processors (item 4) and assembly scanning (item 7) — Conduit already has direct or superior equivalents for both (the `Stage` model natively supports pre/post placement, and `RegisterHandlersAsImplementedFrom`/`RegisterPipesAsImplementedFrom`/`AddValidation()` already do reflection-based registration), so porting MediatR usage of those specific features should be largely mechanical.
