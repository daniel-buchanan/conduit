# Edge Case Review — Validation & Pipe Engine

Audit of `conduit.validation` and the core pipe/conduit engine for edge cases not
covered by the existing test suite. Ambiguous-intent points were put to Daniel via
the grilling process. This is a working register — resolved items still need
implementing + tests.

Status: ✅ all decisions resolved. Nothing implemented yet — this register is the spec to build against.

## Resolved

| # | Location | Decision |
|---|---|---|
| Q1 | `ShouldBuilder.cs:40` (`AddRule`/`Execute`) | `ValidationError.PropertyName` currently holds `request.ToString()`, not the real property name. **Fix**: switch `Should()` to take an `Expression<Func<TRequest,TProperty>>` so the real property name can be extracted. |
| Q2 | `RuleBuilder.cs:25` | `Should(Func<TRequest,string>)` should become nullable: `Func<TRequest,string?>` (combines with Q1 → `Expression<Func<TRequest,string?>>`), removing the need for `!` at every call site. |
| Q3 / Q11 | `ValidationBuilder.cs` | Duplicate validator registration: two+ validator classes discovered for the **same** `(TRequest,TResponse)` pair via `WithValidatorsFromAssembly` (one scan, or across multiple scans) → throws a new `ValidatorAlreadyRegisteredException` (mirrors `PipeAlreadyRegisteredException`). Any explicit `WithValidatorFor` call (instance or type-based) for a pair that already has a registration (from a scan or another explicit call) → always allowed, no throw, last-registered-wins. Only scan-vs-scan collisions throw; any explicit call is presumed intentional. |
| Q4 | `CondiutConfigurationBuilder.cs:29` vs `:100` | `RegisterHandler` should gain the same duplicate-registration guard `RegisterPipe` has (currently asymmetric). |
| Q5 / Q10 | `Pipe.cs:69` `ExecuteStage` catch block | Unexpected exceptions from stages/rules/validators get wrapped as `StageFailedException` (making the currently-dead `StageResult.WithException` constructor live) instead of bubbling raw past `ConduitValidationExceptionHandler`. Exceptions that pass through **unwrapped**: `ValidationFailedException`, `StageFailedException`, and `ValidatorNotFoundException`. `ValidatorNotFoundException` also gets added to `ConduitValidationExceptionHandler`'s `KnownExceptionHandlers` so it produces its own distinct ProblemDetails response instead of a generic "pipeline stage failed" 500. |
| Q6 / Q12 | `BuildablePipe.cs:52` (`response ??= result.Response`) | Later stages **can** override the response (not observer-only): change to unconditional `response = result.Response ?? response`. A verbose log fires only on a **true override** — an earlier stage already set a non-null response, and a later stage supplies a different non-null response that replaces it. The first stage to produce a response is not logged (nothing was overridden yet). |
| Q7 / Q13 | `ReflectionHelper.cs:36-66` `GetRegistrationsAsImplementedFrom` | The fallback that registers a concrete type under itself (when generic args can't be resolved) is a bug. Fix: `RegisterPipesAsImplementedFrom<TLocator>()` throws immediately (e.g. `InvalidOperationException`) at call time when a discovered `IPipe` implementation's closed `TRequest`/`TResponse` can't be determined — fail loud at registration time, don't produce an unreachable pipe. |
| Q8 | `PipeConfigurationRegistry.cs:33` (`Add`) | Should throw (not silently no-op) when called after the registry is locked (post-`Build()`). |
| Q9 / Q9a | `ShouldBuilder.cs` (`Should()` signature) | Expression scope for name extraction: nested member chains are supported (`x => x.Address.City`), walking the full `MemberExpression` chain. `PropertyName` is the **full dotted path** (`"Address.City"`, not just `"City"`) — avoids collisions when two nested properties share a leaf name. The chain must be pure member access all the way down; anything with a method call, indexer, or non-member node anywhere in the chain throws `ArgumentException` at rule-registration time. |

## Clear coverage gaps (no ambiguity — just missing tests, add directly once above is settled)

- `AddValidation()` parameterless overload (assembly scan) — untested.
- `WithValidatorFor<TRequest,TResponse,TModelValidator>()` type-based overload — untested.
- `RegisterPipesAsImplementedFrom<TLocator>` — untested.
- `ReflectionHelper.GetRegistrationsAsImplementedFrom(includeCtorDeps: true)` — unreachable/dead code path.
- `ShouldStringBuilder.NullOrWhitespace` with whitespace-only input (only `null` is tested).
- `ModelValidator` with zero rules added (`AddRules` adds nothing) — untested "valid by default" path.
- `ValidationErrorsExtensions.ToModelState` — completely untested.
- `ConduitValidationExceptionHandler` — completely untested; no test exercises the middleware at all (`ValidationFailedException`, `StageFailedException`, or the unknown-exception passthrough path).
- `DebugPreExecutionStage` / `DebugPostExecutionStage` — registered in DI per-handler but never wired into `DefaultPipeConfiguration`'s stage lists, so they never run by default; untested.
- `HashUtil` — collision/uniqueness of the routing key untested in isolation.
