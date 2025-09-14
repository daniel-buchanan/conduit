using conduit.logging;
using conduit.Pipes.Stages;

namespace conduit;

/// <summary>
/// Provides an abstract base class for request handlers within the Conduit system.
/// This class implements <see cref="IRequestHandler{TRequest, TResponse}"/> and provides a base for pipeline stages.
/// </summary>
/// <typeparam name="TRequest">The type of the request handled by this handler.</typeparam>
/// <typeparam name="TResponse">The type of the response produced by this handler.</typeparam>
/// <param name="logger">The logger instance to be used by the handler.</param>
public abstract class RequestHandler<TRequest, TResponse>(ILog logger) : PipeStage<TRequest, TResponse>(logger), IRequestHandler<TRequest, TResponse>
    where TRequest : class, IRequest<TResponse>
    where TResponse : class
{
    /// <summary>
    /// Executes the internal handling logic for the request.
    /// This method typically calls the <see cref="HandleAsync"/> method.
    /// </summary>
    /// <param name="instanceId">A unique identifier for the current pipe instance.</param>
    /// <param name="request">The request to process.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation, returning the response.</returns>
    protected override async Task<TResponse?> ExecuteInternalAsync(Guid instanceId, TRequest request, CancellationToken cancellationToken) 
        => await HandleAsync(request, cancellationToken);

    /// <summary>
    /// When overridden in a derived class, handles the specified request asynchronously.
    /// This is the core logic that concrete request handlers should implement.
    /// </summary>
    /// <param name="request">The request to handle.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation, returning the response.</returns>
    public abstract Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken = default);
}