using conduit.logging;

namespace conduit.Pipes.Stages;

public abstract class PipeStage<TResponse>(ILog logger) : IPipeStage<TResponse> 
    where TResponse : class
{
    protected ILog Logger => logger;

    public Task<TResponse> ExecuteAsync(Guid instanceId, IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<TResponse> ExecuteAsync(
        Guid instanceId, 
        IRequest<TResponse> request, 
        Func<IRequest<TResponse>, CancellationToken, Task<TResponse>> next, 
        CancellationToken cancellationToken = default)
    {
        logger.Debug("[{0}] IPipeStage<{1}>.ExecuteAsync", instanceId, typeof(TResponse).Name);
        return ExecuteInternalAsync(instanceId, request, next, cancellationToken);
    }
    
    protected abstract Task<TResponse> ExecuteInternalAsync(Guid instanceId, IRequest<TResponse> request, Func<IRequest<TResponse>, CancellationToken, Task<TResponse>> next, CancellationToken cancellationToken);
}