using conduit.logging;
using conduit.Pipes.Stages;

namespace conduit.tests.Stages;

public class LoggingStage<TRequest, TResponse>(ILog logger) : PipeStage<TRequest, TResponse>(logger) 
    where TRequest : class, IRequest<TResponse> 
    where TResponse : class
{
    protected override Task<TResponse?> ExecuteInternalAsync(Guid instanceId, TRequest request, CancellationToken cancellationToken)
    {
        Logger.Debug("[LoggingStage] :: {0} pipe for {1}", instanceId, typeof(TRequest).Name);
        return Task.FromResult<TResponse?>(null);
    }
}