using conduit.logging;

namespace conduit.Pipes.Stages;

/// <summary>
/// Represents a pipe stage that specifically handles the execution of a request using an <see cref="IRequestHandler{TRequest, TResponse}"/>.
/// </summary>
/// <typeparam name="TRequest">The type of the request handled by this stage.</typeparam>
/// <typeparam name="TResponse">The type of the response produced by this stage.</typeparam>
/// <param name="logger">The logger instance to be used by the stage.</param>
/// <param name="handler">The request handler responsible for processing the request.</param>
public class HandleRequestStage<TRequest, TResponse>(
    ILog logger, 
    IRequestHandler<TRequest, TResponse> handler) : PipeStage<TRequest, TResponse>(logger) 
    where TResponse : class
    where TRequest : class, IRequest<TResponse>
{
    /// <summary>
    /// Executes the request handler to process the incoming request.
    /// </summary>
    /// <param name="instanceId">A unique identifier for the current pipe instance.</param>
    /// <param name="request">The request to process.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation, returning the response from the handler.</returns>
    protected override async Task<TResponse?> ExecuteInternalAsync(Guid instanceId, TRequest request, CancellationToken cancellationToken)
    {
        Logger.Debug("[{0}] IRequestHandler<{1}, {2}>.HandleAsync", instanceId, typeof(TRequest).Name, typeof(TResponse).Name);
        return await handler.HandleAsync(request, cancellationToken);
    }
}