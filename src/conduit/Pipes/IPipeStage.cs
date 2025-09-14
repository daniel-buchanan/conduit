namespace conduit.Pipes;

/// <summary>
/// Marker interface for all pipe stages in the Conduit system.
/// </summary>
public interface IPipeStage;

/// <summary>
/// Defines the contract for a stage within a Conduit pipe that processes a request and potentially returns a response.
/// </summary>
/// <typeparam name="TRequest">The type of the request processed by this stage.</typeparam>
/// <typeparam name="TResponse">The type of the response produced by this stage.</typeparam>
public interface IPipeStage<TRequest, TResponse> : IPipeStage
    where TRequest : class, IRequest<TResponse>
    where TResponse: class
{
    /// <summary>
    /// Executes the pipe stage asynchronously without a 'next' delegate. This is typically for terminal stages or stages that don't pass control further.
    /// </summary>
    /// <param name="instanceId">A unique identifier for the current pipe instance.</param>
    /// <param name="request">The request to process.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation, returning the response.</returns>
    Task<TResponse?> ExecuteAsync(
        Guid instanceId, 
        TRequest request, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Executes the pipe stage asynchronously with a 'next' delegate to pass control to the subsequent stage.
    /// </summary>
    /// <param name="instanceId">A unique identifier for the current pipe instance.</param>
    /// <param name="request">The request to process.</param>
    /// <param name="next">A delegate to invoke the next stage in the pipe.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation, returning the response.</returns>
    Task<TResponse?> ExecuteAsync(
        Guid instanceId, 
        TRequest request,
        Func<Guid, TRequest, CancellationToken, Task<TResponse>> next,
        CancellationToken cancellationToken = default);
}