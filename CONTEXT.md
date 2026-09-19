# Conduit

A MediatR-style request/response mediator for .NET: consumers define `IRequest<TResponse>` messages and `IRequestHandler` implementations, and `IConduit` routes a request to its handler through a configurable pipeline, without callers taking a direct dependency on the handler.

## Language

**Conduit**:
The single facade a consumer depends on to send a request and await its response. Resolves the right `Pipe` for the request/response type pair at runtime.

**Push**:
The verb for sending a request through the Conduit and awaiting its response (`PushAsync`). Deliberately not called `Send` (MediatR's term) — Conduit leans into its own plumbing metaphor (Conduit → Pipe → Stage), where you push a request down a pipe.
_Avoid_: Send, Dispatch.

**Pipe**:
The full ordered sequence of `Stage`s that a single `(TRequest, TResponse)` type pair runs through, from pre-execution through the `Handler` to post-execution. Exactly one `Pipe` may be registered per type pair (`PipeAlreadyRegisteredException` on duplicate) — Conduit is a strict 1:1 request/response mediator, not a pub/sub event bus.
_Avoid_: Pipeline (used interchangeably in comments, but "Pipe" is the canonical term), Handler chain.

**Stage**:
One unit of work within a `Pipe`. Stages execute as a flat, ordered sequence — each stage runs to completion before the next starts. There is no onion-style middleware chaining (a stage cannot itself decide whether to invoke "the next stage"); see [ADR-0002](docs/adr/0002-flat-sequential-stage-execution.md).
_Avoid_: Middleware, filter.

**Handler**:
The stage that contains the request's actual business logic (`IRequestHandler<TRequest,TResponse>`/`RequestHandler<TRequest,TResponse>`). A Handler *is* a Stage — it implements `IPipeStage` directly rather than being wrapped by a separate adapter stage.

**Descriptor**:
The config-time recipe for something the `Conduit` will construct or register (`PipeDescriptor`, `StageDescriptor`) — as opposed to a `Result` (`StageResult`, `DebugResult`), which is what a `Stage` or `Pipe` produces at runtime.

**Registry**:
The locked, startup-time store of `PipeDescriptor`s keyed by `(TRequest, TResponse)` (`IPipeConfigurationRegistry`). Populated once during `Build()` and then `Lock()`ed — it never evicts, expires, or recomputes entries, so it's a registry, not a cache.
_Avoid_: Cache — reads as transient/re-derivable data, which this isn't.

**ModelValidator**:
The per-`(TRequest, TResponse)` validation contract (`IModelValidator<TRequest, TResponse>`) an app registers to validate a request before it reaches its `Handler`. Runs inside `ValidationStage`, one of the default pipe stages. Resolved from DI by request/response type pair; if none is registered, either passes silently or throws `ValidatorNotFoundException`, depending on `ThrowIfValidatorNotFound`.

**Rule**:
One property's full validation spec, built through `RuleBuilder.Should(...)` and stored as a `Rule<TRequest>` inside a `ModelValidator`. A single property can carry multiple Rules (e.g. `NotBe().Null()` and `Be().EqualTo(...)` on the same property), and a `ModelValidator` runs all its Rules, aggregating every failure into `ValidationResult.Errors` rather than stopping at the first one.
_Avoid_: Condition, for this level — a Rule *contains* a Condition, it isn't one.

**Condition**:
The terminal check inside a `Rule` — `Null`, `EqualTo`, `In`, `NullOrWhitespace`, etc. Selected via `Be()` (assert the condition holds) or `NotBe()` (assert it doesn't).
_Avoid_: Rule, for this level (see Rule).

**RuleBuilder**:
Fluent entry point (`IRuleBuilder<TRequest>`) a `ModelValidator` uses inside `AddRules` to declare its Rules: `Should(property)` → `Be()`/`NotBe()` → a terminal `Condition` method.

**Should**:
The verb that starts a `Rule`, selecting the property via a member-access expression tree so `PropertyName` on a resulting `ValidationError` is the real member name, not a guess (see [ADR-0006](docs/adr/0006-property-name-via-expression-trees.md)). Must be a pure member-access chain (`x => x.Foo` or `x => x.Foo.Bar`); anything else (a method call, an indexer) throws `ArgumentException` at registration time.

**In**:
The `Condition` asserting a property's value is contained in a supplied set.
_Avoid_: OneOf — identical behavior, kept only as a pre-existing alias for callers already using it. New Rules should use `In`.

**ValidationResult / ValidationError**:
The outcome of running a `ModelValidator` (or a single `Rule`) against a request: `IsValid`, plus on failure one `ValidationError` (`PropertyName` + `Message`) per failed `Condition` — a `ModelValidator` with several failing Rules produces one error per failure, not just the first.

**ValidationStage**:
The default pre-execution `Stage` that resolves and runs the registered `ModelValidator` for a `Pipe`'s `(TRequest, TResponse)` pair. Implements `IValidationPipeStage` so `PipeFactory` can skip it for Pipes configured with `ExcludeValidation`.

**Notification**:
Not yet implemented. A future one-event-to-many-handlers concept (MediatR's `INotification`/`Publish`), distinct from the strict 1:1 `Pipe`. Known gap, not yet designed — when it lands, its vocabulary must not collide with `Pipe`/`Stage`.
