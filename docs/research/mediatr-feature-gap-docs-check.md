# Docs check: `mediatr-feature-gap.md` against MediatR's primary sources

Checked 2026-11-26 against MediatR `main` (latest release v15.0.0, 2026-11-12). Sources: README, the official docs (the wiki now redirects to docs.mediatr.io, whose source is `docs/source/*.md` in the repo; read raw from there), source under `src/`, and GitHub releases 12.3 through 15.0. Every source fetched; none failed.

## Verdict

**No major feature is missing outright.** The big items (behaviors, notifications, streaming, runtime-typed send, void requests, value-type responses) are all in the gap doc. The gaps it misses are in the fine print of features it already marks "covered": constrained open-generic behaviors, open-generic and polymorphic exception handlers, and the default order of exception actions vs. handlers. Together these mean item 5's "full equivalent, and broader" overstates parity. Two factual claims are wrong: the one about handler lifetime, and the one saying MediatR's exception handlers wrap only the handler.

## Missing from the gap doc

### 1. Constrained open-generic behaviors and processors (medium)

**MediatR**: an open behavior with a generic constraint, such as `TransactionBehavior<TRequest,TResponse> where TRequest : ICommand`, runs only for requests that satisfy the constraint. Requests that don't match just don't get it. The docs register a `ConstrainedBehavior<,>` with `AddOpenBehavior` (https://raw.githubusercontent.com/LuckyPennySoftware/MediatR/main/docs/source/Behaviors.md) and list "Constrained Post-Processor"/"Constrained Notification Handler" as supported on MS DI (https://raw.githubusercontent.com/LuckyPennySoftware/MediatR/main/docs/source/Container-Feature-Support.md). Since 14.3, MediatR closes behaviors with nested or extra type parameters itself, inferring them from constraints. `OpenBehaviorClosingStrategy` chooses whether MediatR or the container does the closing (README "Open behaviors with nested or extra type parameters", https://raw.githubusercontent.com/LuckyPennySoftware/MediatR/main/README.md; https://github.com/LuckyPennySoftware/MediatR/releases/tag/v14.3.0; `src/MediatR/MicrosoftExtensionsDI/OpenBehaviorClosingStrategy.cs`).

**Conduit**: `AddDefaultPreExecutionStage`/`AddDefaultPostExecutionStage` apply a stage to every pipe. `StageMaterializer.Materialize` (`src/conduit/Helpers/StageMaterializer.cs`) calls `MakeGenericType(request, response[, handler])` without checking constraints. By inspection (not run), a constrained default stage would throw `ArgumentException` for any request that doesn't satisfy the constraint. Today the only ways to scope a stage to a subset of requests are per-pipe `RegisterPipe` or a runtime type check inside the stage.

**Why medium**: "this behavior applies only to commands / transactional requests" is a common MediatR idiom. It is distinct from the onion-vs-flat question (item 3), because it matters even for pre/post stages, which item 4 counts as parity.

### 2. Open-generic, cross-request exception handlers and actions (medium)

**MediatR**: "This also registers open generic implementations for … `IRequestExceptionHandler<,,>`, `IRequestExceptionAction<,>`" (README, https://raw.githubusercontent.com/LuckyPennySoftware/MediatR/main/README.md). The scan passes `addIfAlreadyExists: true` for both and closes open generics (`src/MediatR/Registration/ServiceRegistrar.cs`, https://raw.githubusercontent.com/LuckyPennySoftware/MediatR/main/src/MediatR/Registration/ServiceRegistrar.cs). With this, one `LogAll<TRequest,TException>` covers every request.

**Conduit**: `RegisterExceptionHandlersFromAssemblyContaining` throws `InvalidOperationException` on a generic type, and its docs say "register each closed form by hand" (`src/conduit/IConduitConfigurationBuilder.cs`). Manually registering an open generic through `AddDescriptor` is untested. Because `ExceptionHandlerRunner` resolves one closed type per exception ancestor (`src/conduit/Pipes/ExceptionHandlerRunner.cs`), an arity-matching open type would probably run once per ancestor.

**Why medium**: catch-all exception logging or mapping is a common reason to use these interfaces at all.

### 3. Exception actions vs. handlers: default order (medium)

**MediatR**: `RequestExceptionActionProcessorStrategy` defaults to `ApplyForUnhandledExceptions`. The action behavior is registered outside the handler behavior, so **actions run only when no handler handled the exception**. `ApplyForAllExceptions` is opt-in (https://raw.githubusercontent.com/LuckyPennySoftware/MediatR/main/docs/source/Exception-handling.md; `src/MediatR/MicrosoftExtensionsDI/RequestExceptionActionProcessorStrategy.cs`; `ServiceRegistrar.AddRequiredServices`).

**Conduit**: `ExceptionHandlerRunner.RunAsync` always runs every action first and then the handlers (`src/conduit/Pipes/ExceptionHandlerRunner.cs`). That is MediatR's opt-in `ApplyForAllExceptions` behavior, and there is no switch. ADR-0016 says actions "always run in full" but doesn't contrast this with MediatR's default.

**Why medium**: a straight port changes behavior silently. Exceptions a handler recovers from will now also fire actions, such as alerting or error logging.

### 4. Polymorphic (contravariant) dispatch (minor now, medium once notifications exist)

**MediatR**: the handler interfaces are contravariant: `IRequestHandler<in TRequest,…>`, `INotificationHandler<in TNotification>`, `IRequestExceptionHandler<in TRequest, TResponse, in TException>`, `IRequestExceptionAction<in TRequest, in TException>`, `IRequestPreProcessor<in TRequest>`. For example, an `INotificationHandler<INotification>` receives every notification (https://raw.githubusercontent.com/LuckyPennySoftware/MediatR/main/docs/source/Polymorphic-dispatch.md). Container support varies. On MS DI, covariant notification handlers work, but covariant exception handlers/actions **do not** (https://raw.githubusercontent.com/LuckyPennySoftware/MediatR/main/docs/source/Container-Feature-Support.md).

**Conduit**: every interface is invariant (`src/conduit/IRequestHandler.cs`, `src/conduit/Exceptions/Handling/IRequestExceptionHandler.cs`, `IRequestExceptionAction.cs`), and the runner closes on the exact `typeof(TRequest)`.

**Why minor**: on MS DI, the main payoff is base-type notification handlers. Notifications are already roadmap item 4, so this belongs in that design.

### 5. `MediatR.Contracts` contracts-only package (minor)

**MediatR**: `IRequest`, `INotification` and `IStreamRequest` ship in a dependency-free `MediatR.Contracts` package (netstandard2.0), for request DTOs that live in API, gRPC or Blazor contract assemblies (README "Using Contracts-Only Package"; https://raw.githubusercontent.com/LuckyPennySoftware/MediatR/main/docs/source/Migration-Guide-14.x-to-15.0.md).

**Conduit**: `IRequest<T>` lives in `conduit` core (`src/conduit/IRequest.cs`), which references `Microsoft.Extensions.DependencyInjection.Abstractions` and `conduit.logging` (`src/conduit/conduit.csproj`). A shared contracts assembly therefore takes on the whole core.

**Why minor**: it's a packaging concern, not a capability gap.

### 6. Exception handler priority and override rules (minor)

**MediatR**: when several handlers or actions match, MediatR sorts them by assembly and namespace proximity to the request. A subclass of a handler overrides its base (https://raw.githubusercontent.com/LuckyPennySoftware/MediatR/main/docs/source/Exception-handling.md; `src/MediatR/Internal/HandlersOrderer.cs`, `ObjectDetails.cs`). The behavior also walks the exception hierarchy most-derived first and stops at the first handler that handles it (`src/MediatR/Pipeline/RequestExceptionProcessorBehavior.cs`).

**Conduit**: Conduit gathers handlers for every ancestor and sorts them by an explicit required `Order` only (`ExceptionHandlerRunner.cs`). A handler for `Exception` with a lower `Order` therefore beats one for `ArgumentException`. MediatR would try the `ArgumentException` handler first regardless.

**Why minor**: Conduit's explicit `Order` is arguably clearer. But porting needs deliberate `Order` values to keep MediatR's "most specific exception wins" semantics.

### 7. Replaceable mediator implementation (minor)

**MediatR**: `MediatorImplementationType` swaps the `IMediator` implementation, and `Mediator.PublishCore` is `protected virtual` (`src/MediatR/MicrosoftExtensionsDI/MediatrServiceConfiguration.cs`, `src/MediatR/Mediator.cs`). Registration uses `TryAdd`, so a prior registration wins.

**Conduit**: `AddConduit` uses `AddTransient<IConduit, Conduit>()` (`src/conduit/ServiceCollectionExtensions.cs`). A consumer can override it by re-registering afterwards; there's no configuration hook.

**Why minor**: few consumers need it, and the DI workaround is straightforward.

## Corrections to the gap doc

1. **Item 10, handler lifetime**: "handler lifetime is fixed at Scoped … where MediatR offers `cfg.Lifetime`" is wrong about MediatR. `cfg.Lifetime` (default Transient) governs only `IMediator`/`ISender`/`IPublisher` and a type-registered `INotificationPublisher` (`ServiceRegistrar.AddRequiredServices`). Scanned handlers are **always transient**: `ConnectImplementationsToTypesClosing` uses `AddTransient`/`TryAddTransient` (https://raw.githubusercontent.com/LuckyPennySoftware/MediatR/main/src/MediatR/Registration/ServiceRegistrar.cs; Setup docs, "concrete implementations as transient", https://raw.githubusercontent.com/LuckyPennySoftware/MediatR/main/docs/source/Setup.md). Behaviors and processors take a per-call `ServiceLifetime`. So MediatR has no configurable *handler* lifetime either. The real difference is Transient (MediatR) vs. Scoped (Conduit).
2. **Item 5, "MediatR has no equivalent, because its behaviors wrap only the handler call"**: wrong. `AddRequiredServices` registers `RequestExceptionProcessorBehavior`/`RequestExceptionActionProcessorBehavior` *before* the pre/post-processor behaviors and `cfg.AddBehavior`/`AddOpenBehavior` behaviors. `RequestHandlerWrapper` nests behaviors outer-to-inner in registration order. The exception behaviors are therefore outermost and catch exceptions from pre/post-processors and other behaviors too (the docs say "after an exception is thrown by the following pipeline steps", Exception-handling.md). Conduit's distinctive feature is *separate* typed interfaces for handler vs. non-handler failures, not the ability to intercept non-handler failures at all.
3. **Item 5, "Full equivalent"**: qualify with missing items 2, 3 and 6 above.
4. **Item 9, licensing**: "current MediatR requires a license key" overstates it. Validation runs on a background thread, is "logging-only — it gates no Mediator behavior" (comment in `src/MediatR/MicrosoftExtensionsDI/MediatRServiceCollectionExtensions.cs`), and the README shows the warning can be filtered off. Also new since the doc was written: `LicenseKeyFactory` (v14.3.0) and a 30-day renewal grace period (README).
5. **Source URLs**: the doc cites `raw.githubusercontent.com/jbogard/MediatR/master/README.md`. The canonical copy is `LuckyPennySoftware/MediatR/main/README.md`, and the wiki is frozen and redirects to docs.mediatr.io. Future citations should use those.

## Checked, already covered (no change needed)

- `ISender` surface: `Send<T>`, void `Send<TRequest>`, `Send(object)`, `CreateStream<T>`, `CreateStream(object)` (`src/MediatR/ISender.cs`). `CreateStream(object)` is implied by items 6 and 10.
- `IPublisher.Publish(object)` and `Publish<T>` (`src/MediatR/IPublisher.cs`), covered with notifications (item 2). Since v14.1.0, notification handlers are de-duplicated by type (`Wrappers/NotificationHandlerWrapper.cs`), a detail for when notifications get designed.
- `INotificationPublisher` with `ForeachAwaitPublisher`/`TaskWhenAllPublisher`, plus `NotificationPublisher`/`NotificationPublisherType` (Notifications.md).
- Behavior ordering = registration order, outer to inner (Behaviors.md); `AddStreamBehavior`/`AddOpenStreamBehavior`; `AddRequestPreProcessor`/`AddOpenRequestPreProcessor`/`PostProcessor` equivalents; `AutoRegisterRequestProcessors` (`MediatrServiceConfiguration.cs`).
- Generic handler limits. Since v15.0.0, `0` disables `MaxGenericTypeParameters`/`MaxTypesClosing`/`MaxGenericTypeRegistrations`/`RegistrationTimeout` (Generic-requests-and-handlers.md).
- `IMediator`/`ISender`/`IPublisher` registered transient (README, Setup.md). Conduit's `IConduit` is also transient (`ServiceCollectionExtensions.cs`).
- `TypeEvaluator` and multiple assemblies, `Unit`, `IBaseRequest`/covariance, streaming, licensing env vars.
- **Not in MediatR at all**: OpenTelemetry/`ActivitySource`/diagnostics, Native AOT or source generators. No such types exist in `src/` (full tree listed via the GitHub API), and none appear in the docs or in releases 12.3 through 15.0. Not a gap. MediatR does now *require* an `ILoggerFactory` in the container (Setup.md), but only for licensing logs.
- Release notes 12.3 through 15.0 (https://github.com/LuckyPennySoftware/MediatR/releases): 15.0 targets net10/net11/netstandard2.0/net462. 13.x added netstandard2.0 and the license key. No new dispatch features beyond those above.
