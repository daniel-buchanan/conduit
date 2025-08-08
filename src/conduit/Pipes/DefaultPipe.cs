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
        Func<IRequest<TResponse>, CancellationToken, Task<TResponse>> handlerFunc = (req, ct)
            => handler.ExecuteAsync(instanceId, req, (r, c) => post.ExecuteAsync(instanceId, r, c), ct);
        return await pre.ExecuteAsync(
            instanceId,
            request,
            handlerFunc,
            cancellationToken);
    }
}