using conduit.logging;

namespace conduit.Pipes.Stages;

public class HandleRequestStage<TRequest, TResponse>(
    ILog logger, 
    IRequestHandler<TRequest, TResponse> handler) : PipeStage<TResponse>(logger) 
    where TResponse : class
    where TRequest : IRequest<TResponse>
{
    protected override async Task<TResponse?> ExecuteInternalAsync(
        Guid instanceId, 
        IRequest<TResponse> request,
        CancellationToken cancellationToken)
    {
        Logger.Debug("[{0}] IRequestHandler<{1}, {2}>.HandleAsync", instanceId, typeof(TRequest).Name, typeof(TResponse).Name);
        var result = await handler.HandleAsync(request, cancellationToken);
        return await next(request, cancellationToken);
    }
}