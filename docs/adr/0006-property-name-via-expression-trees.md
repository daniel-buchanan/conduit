# Property names for validation errors come from expression trees, not ToString()

`ValidationError.PropertyName` was populated from `request.ToString()` inside `AbstractShouldBuilder.AddRule` — never the property actually being validated. The root cause is that `Should()` only ever received a `Func<TRequest, TProperty>`, a compiled delegate with no way to recover which member it read.

We change `Should()` to take an `Expression<Func<TRequest, TProperty>>` instead of a plain `Func`, so the real member access can be extracted from the expression tree at rule-registration time before it's compiled to a delegate for execution. The string-returning overload (`RuleBuilder.cs:25`) becomes nullable — `Expression<Func<TRequest, string?>>` — removing the null-forgiving `!` that call sites previously needed.

Extraction supports nested member chains (`x => x.Address.City`), not just top-level access, by walking the full `MemberExpression` chain. `PropertyName` is set to the full dotted path (`"Address.City"`, not just `"City"`), which avoids collisions when two nested properties share a leaf name. The chain must be pure member access all the way down — a method call, an indexer, or any other expression node anywhere in the chain is rejected with `ArgumentException` at rule-registration time, rather than silently falling back to a wrong or empty name.
