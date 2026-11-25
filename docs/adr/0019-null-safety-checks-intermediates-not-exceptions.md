# Nested-chain null safety checks intermediates instead of catching exceptions

[ADR-0013](0013-null-safe-nested-property-chains.md) made compiled `Should()` expressions null-safe by catching `NullReferenceException` around the whole delegate, on the grounds that a pure member-access chain contains "no arbitrary user code". That premise is wrong: every property in the chain is a getter, and getters are user code. A getter with its own bug — dereferencing a null field, say — threw a `NullReferenceException` that the catch swallowed, so the rule silently saw `null` and either passed or reported a misleading "must not be null" error instead of surfacing the bug.

`PropertyNameExtractor.ExtractAndCompile` now rebuilds the chain as an expression tree that tests each nullable intermediate before accessing the next member and returns `default(TProperty)` the moment one is null. The observable behaviour ADR-0013 chose is unchanged — a null intermediate still evaluates the same as a null leaf — but there is no `catch`, so any exception thrown inside a getter propagates.

Keeping the `catch` and trying to tell the two NRE sources apart (e.g. by inspecting the stack trace) was rejected: it is fragile, and the explicit null checks are both simpler and cheaper than unwinding an exception on every null path.
