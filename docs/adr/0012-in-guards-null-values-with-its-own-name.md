# `In`/`OneOf` guard `null` values with their own parameter name

`AbstractShouldBuilder.In` compiled its rule as `r => values.Contains(property(r))`. If `values` is null, `Enumerable.Contains` does throw `ArgumentNullException` — it already guards its own `source` parameter — but the exception names `source`, LINQ's internal parameter name, not `values`, the name a caller of `.In(values)` actually used. Debugging a validator from that message alone gives no clue which rule or argument is at fault.

We added an explicit `ArgumentNullException.ThrowIfNull(values, nameof(values))` guard inside the rule closure, ahead of the `.Contains` call, so the exception (still `ArgumentNullException`, still thrown only once the rule executes — never at `.In()`/`.OneOf()` call time, since a rule is just registered then, not run) names the parameter the validator author actually wrote.

Out of scope here: `ModelValidator.Validate()` (the synchronous entry point) wraps any exception thrown during rule execution in `AggregateException` via `Task.Wait()`. That's a pre-existing, broader behavior affecting every rule's exceptions, not specific to `In`/`OneOf`, and changing it would need its own decision about whether synchronous callers should ever see an unwrapped exception at all.
