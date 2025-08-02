using conduit.logging;
using conduit.Pipes.Stages;

namespace conduit.Pipes;

public class DefaultPipe<TRequest, TResponse>(
    ILog logger, 
    DebugPreExecutionStage<TResponse> pre,
    HandleRequestStage<TRequest, TResponse> handler,
    DebugPostExecutionStage<TResponse> post)
    : IPipe<TResponse>
    where TResponse : class 
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> SendAsync(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        var instanceId = Guid.NewGuid();
        logger.Debug("[{0}] IPipe<{1}>.ExecuteAsync", instanceId, typeof(TResponse).Name);
        return await pre.ExecuteAsync(
            instanceId, 
            request, 
            handler.ExecuteAsync, 
            cancellationToken);
    }
}