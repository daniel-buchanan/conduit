using conduit.logging;

namespace conduit.Pipes.Stages;

public abstract class PipeStage<TResponse>(ILog logger) : IPipeStage<TResponse> 
    where TResponse : class
{
    protected ILog Logger => logger;

    public async Task<TResponse?> ExecuteAsync(Guid instanceId, IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        logger.Debug("[{0}] IPipeStage<{1}>.ExecuteAsync", instanceId, typeof(TResponse).Name);
        return await ExecuteInternalAsync(instanceId, request, cancellationToken);
    }
    
    protected abstract Task<TResponse?> ExecuteInternalAsync(Guid instanceId, IRequest<TResponse> request, CancellationToken cancellationToken);
}