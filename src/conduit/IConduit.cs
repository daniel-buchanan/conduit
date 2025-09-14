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
    Task<TResponse?> PushAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        where TResponse : class;
}