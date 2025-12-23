namespace conduit.Pipes;

/// <summary>
/// Marker interface for all Conduit pipes.
/// </summary>
public interface IPipe;

/// <summary>
/// Defines the contract for a Conduit pipe that processes a request and returns a response.
/// </summary>
/// <typeparam name="TRequest">The type of the request to be processed by the pipe.</typeparam>
/// <typeparam name="TResponse">The type of the response produced by the pipe.</typeparam>
public interface IPipe<in TRequest, TResponse> : IPipe
    where TResponse : class
    where TRequest : class, IRequest<TResponse>
{
    /// <summary>
    /// Pushes a request through the pipe and awaits a response.
    /// </summary>
    /// <param name="request">The request to send.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation, returning the response.</returns>
    Task<TResponse?> PushAsync(TRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Pushes a request through the pipe and awaits a response. While timing and recording information about each stage.
    /// </summary>
    /// <param name="request">The request to send.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation, returning the response wrapped in debugging information.</returns>
    Task<DebugResult<TResponse?>> PushWithDebugAsync(TRequest request, CancellationToken cancellationToken = default);
}