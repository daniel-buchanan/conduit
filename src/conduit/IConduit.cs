using conduit.Exceptions;
using conduit.Pipes;

namespace conduit;

/// <summary>
/// Represents the main interface for sending requests through the Conduit system.
/// </summary>
public interface IConduit
{
    /// <summary>
    /// Pushes a request through the Conduit pipeline and awaits a response.
    /// </summary>
    /// <typeparam name="TResponse">The type of the response expected from the request.</typeparam>
    /// <param name="request">The request to be processed.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation, returning the response.</returns>
    /// <exception cref="PipeNotFoundException">Thrown when a suitable pipe for the request type cannot be found.</exception>
    Task<TResponse?> PushAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        where TResponse : class;
    
    /// <summary>
    /// Pushes a request through the Conduit and awaits a response. While timing and recording information about each stage.
    /// </summary>
    /// <typeparam name="TResponse">The type of the response expected from the request.</typeparam>
    /// <param name="request">The request to send.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation, returning the response wrapped in debugging information.</returns>
    /// <exception cref="PipeNotFoundException">Thrown when a suitable pipe for the request type cannot be found.</exception>
    Task<DebugResult<TResponse?>> PushWithDebugAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        where TResponse : class;
}