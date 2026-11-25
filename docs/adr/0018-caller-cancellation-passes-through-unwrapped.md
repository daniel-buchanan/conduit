# Cancellation of the caller's token passes through unwrapped

[ADR-0004](0004-wrap-unexpected-stage-exceptions.md) wraps every unexpected stage exception as `StageFailedException`, exempting only Conduit's own passthrough types. That swept up `OperationCanceledException` too: when the caller cancelled a push (an ASP.NET Core request abort, a shutting-down worker), the cancellation surfaced as a "stage failed" error and, behind `ConduitValidationExceptionHandler`, as a 500 — reporting the caller's own decision to stop as a server fault, and logging it at Error level.

`Pipe.ExecuteStage` now rethrows an `OperationCanceledException` untouched when the `CancellationToken` passed to the push has been cancelled. Hosts and callers already treat that exception as "the operation was abandoned", so it keeps its standard meaning.

The passthrough is deliberately conditional on the caller's token rather than exempting every `OperationCanceledException`. A stage can also see one that has nothing to do with the caller — an `HttpClient` timeout throws `TaskCanceledException` — and that is a genuine stage failure, so it is still wrapped per ADR-0004. Exempting the type unconditionally was rejected because it would let internal timeouts masquerade as caller cancellations.
