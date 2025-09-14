using conduit.Pipes;

namespace conduit;

/// <summary>
/// Marker interface for all request handlers in the Conduit system.
/// </summary>
public interface IRequestHandler;

/// <summary>
/// Defines the contract for a request handler that processes a specific type of request and returns a response.
/// This interface also acts as a pipe stage for integration into the Conduit pipeline.
/// </summary>
/// <typeparam name="TRequest">The type of the request to handle.</typeparam>
/// <typeparam name="TResponse">The type of the response produced by the handler.</typeparam>
public interface IRequestHandler<TRequest, TResponse> 
    : IPipeStage<TRequest, TResponse>, IRequestHandler
    where TRequest : class, IRequest<TResponse>
    where TResponse : class
{
    /// <summary>
    /// Handles the specified request asynchronously.
    /// </summary>
    /// <param name="request">The request to handle.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation, returning the response.</returns>
    Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken = default);
}