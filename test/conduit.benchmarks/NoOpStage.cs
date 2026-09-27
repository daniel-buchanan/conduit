using conduit.logging;
using conduit.Pipes.Stages;

namespace conduit.benchmarks;

/// <summary>
/// A pipe stage that does nothing, used to isolate the fixed per-stage cost (DI resolution,
/// logging, stage-type name formatting) from any work a real stage like validation performs.
/// </summary>
public class NoOpStage<TRequest, TResponse>(ILog logger) : PipeStage<TRequest, TResponse>(logger)
    where TRequest : class, IRequest<TResponse>
    where TResponse : class
{
    protected override Task<StageResult<TRequest, TResponse>> ExecuteInternalAsync(Guid instanceId, TRequest request, CancellationToken cancellationToken)
        => Task.FromResult(StageResult.WithIndeterminateResult<TRequest, TResponse>(GetType()));
}
