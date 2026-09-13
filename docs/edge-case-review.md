# Edge Case Review — Validation & Pipe Engine

Audit of `conduit.validation` and the core pipe/conduit engine for edge cases not
covered by the existing test suite. Ambiguous-intent points were put to Daniel via
the grilling process. This is a working register — resolved items still need
implementing + tests; open items still need answers.

Status key: ✅ resolved (decision made, not yet implemented) · 🔲 open (needs an answer)

## Resolved (Round 1)

| # | Location | Decision |
|---|---|---|
| Q1 | `ShouldBuilder.cs:40` (`AddRule`/`Execute`) | `ValidationError.PropertyName` currently holds `request.ToString()`, not the real property name. **Fix**: switch `Should()` to take an `Expression<Func<TRequest,TProperty>>` so the real property name can be extracted. |
| Q2 | `RuleBuilder.cs:25` | `Should(Func<TRequest,string>)` should become nullable: `Func<TRequest,string?>` (combines with Q1 → `Expression<Func<TRequest,string?>>`), removing the need for `!` at every call site. |
| Q3 | `ValidationBuilder.cs` | Duplicate validator registration for the same `(TRequest,TResponse)` pair should throw — **but** explicit `WithValidatorFor` calls must be allowed to override. |
| Q4 | `CondiutConfigurationBuilder.cs:29` vs `:100` | `RegisterHandler` should gain the same duplicate-registration guard `RegisterPipe` has (currently asymmetric). |
| Q5 | `Pipe.cs:69` `ExecuteStage` catch block | Unexpected exceptions from stages/rules/validators should be wrapped as `StageFailedException` (making the currently-dead `StageResult.WithException` constructor live) instead of bubbling raw past `ConduitValidationExceptionHandler`. |
| Q6 | `BuildablePipe.cs:52` (`response ??= result.Response`) | Later stages **should** be able to override the response (not observer-only) — but must emit a verbose log when they do. |
| Q7 | `ReflectionHelper.cs:36-66` `GetRegistrationsAsImplementedFrom` | The fallback that registers a concrete type under itself (when generic args can't be resolved) is a bug, not a deliberate degraded mode — `RegisterPipesAsImplementedFrom` should fail clearly instead of producing an unreachable pipe. |
| Q8 | `PipeConfigurationRegistry.cs:33` (`Add`) | Should throw (not silently no-op) when called after the registry is locked (post-`Build()`). |

## Open (Round 2 — follow-ups unlocked by Round 1 answers)

🔲 **Q9 — Property-name expression scope.** Now that `Should()` becomes `Expression<Func<TRequest,TProperty>>` (Q1), what expression shapes are supported for extracting the name?
- (a) *(recommended)* Simple top-level member access only (`x => x.Foo` → `"Foo"`). Anything else (`x => x.Foo.Bar`, `x => x.GetFoo()`, `x => x.Foo ?? "d"`) throws `ArgumentException` at rule-registration time.
- (b) Also support nested chains (`x => x.Address.City`), walking the full `MemberExpression` chain.

🔲 **Q10 — Which exceptions pass through unwrapped vs get wrapped as `StageFailedException`?** Per Q5, the catch block in `Pipe.ExecuteStage` needs an explicit "known, don't wrap" list. `ValidatorNotFoundException` (thrown by `ValidationStage` when `ThrowIfValidatorNotFound()` is set and no validator is found) flows through the same catch block and today bubbles raw/unhandled.
- (a) Only `ValidationFailedException` and `StageFailedException` pass through raw; `ValidatorNotFoundException` gets wrapped into a generic `StageFailedException` like anything else unexpected.
- (b) *(recommended)* `ValidatorNotFoundException` also passes through raw, **and** gets added to `ConduitValidationExceptionHandler`'s `KnownExceptionHandlers` so it produces its own distinct ProblemDetails response instead of a generic "pipeline stage failed" 500.

🔲 **Q11 — Exact throw/override split for duplicate validators (Q3 follow-up).** Proposed concrete rule:
- Two+ validator classes discovered for the **same** `(TRequest,TResponse)` pair via `WithValidatorsFromAssembly` (one scan or across multiple scans) → throws (new `ValidatorAlreadyRegisteredException`, mirrors `PipeAlreadyRegisteredException`).
- Any explicit `WithValidatorFor` call (instance or type-based) for a pair that already has a registration (from a scan or another explicit call) → always allowed, no throw, last-registered-wins.
- Open question: should two explicit `WithValidatorFor` calls for the *same* pair also throw, or is "any explicit call is presumed intentional, never throws" (as above) correct? *(recommended: as stated — only scan-vs-scan throws)*

🔲 **Q12 — When does the Q6 override-log fire?**
- (a) *(recommended)* Only on a true override — an earlier stage already set a non-null response, and a later stage supplies a different non-null response that replaces it. The first stage to produce a response isn't logged (nothing was overridden).
- (b) Every time any stage supplies a non-null response, including the first.

🔲 **Q13 — `RegisterPipesAsImplementedFrom`: throw immediately, or skip-with-warning (Q7 follow-up)?**
- (a) *(recommended)* Throw immediately (e.g. `InvalidOperationException`) at `RegisterPipesAsImplementedFrom<TLocator>()` call time when a discovered `IPipe` implementation's closed `TRequest`/`TResponse` can't be determined.
- (b) Skip that type, log a `Warn`, continue registering the rest.

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
