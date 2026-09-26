using conduit.logging;
using conduit.Pipes.Stages;

namespace conduit.tests.Stages;

public class LoggingStage<TRequest, TResponse>(ILog logger) : PipeStage<TRequest, TResponse>(logger) 
    where TRequest : class, IRequest<TResponse> 
    where TResponse : class
{
    protected override Task<StageResult<TRequest, TResponse>> ExecuteInternalAsync(Guid instanceId, TRequest request, CancellationToken cancellationToken)
    {
        Logger.Debug($"[LoggingStage] :: {instanceId} pipe for {typeof(TRequest).Name}");
        var result = StageResult.WithIndeterminateResult<TRequest, TResponse>(this.GetType());
        return Task.FromResult(result);
    }
}