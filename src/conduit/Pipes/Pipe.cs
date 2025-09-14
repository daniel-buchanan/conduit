namespace conduit.Pipes;

/// <summary>
/// Provides an abstract base class for Conduit pipes, defining the core behavior for sending requests.
/// </summary>
/// <typeparam name="TRequest">The type of the request processed by this pipe.</typeparam>
/// <typeparam name="TResponse">The type of the response produced by this pipe.</typeparam>
public abstract class Pipe<TRequest, TResponse> : IPipe<TRequest, TResponse> 
    where TResponse : class 
    where TRequest : class, IRequest<TResponse>
{
    /// <summary>
    /// Sends a request through the pipe asynchronously. Derived classes must implement this method
    /// to define the specific pipeline logic.
    /// </summary>
    /// <param name="request">The request to send.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation, returning the response.</returns>
    public virtual Task<TResponse?> SendAsync(TRequest request, CancellationToken cancellationToken = default)
    {
        var instanceId = Guid.NewGuid();
        var context = new PipeContext<TResponse>(0, request);
        throw new NotImplementedException();
        
    }
}