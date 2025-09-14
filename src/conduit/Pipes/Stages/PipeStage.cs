using conduit.logging;

namespace conduit.Pipes.Stages;

/// <summary>
/// Provides an abstract base class for implementing pipe stages in the Conduit system.
/// </summary>
/// <typeparam name="TRequest">The type of the request processed by this stage.</typeparam>
/// <typeparam name="TResponse">The type of the response produced by this stage.</typeparam>
/// <param name="logger">The logger instance to be used by the stage.</param>
public abstract class PipeStage<TRequest, TResponse>(ILog logger) : IPipeStage<TRequest, TResponse>
    where TResponse : class
    where TRequest : class, IRequest<TResponse>
{
    /// <summary>
    /// Gets the logger instance used by this pipe stage.
    /// </summary>
    protected ILog Logger => logger;
    
    /// <summary>
    /// When overridden in a derived class, executes the internal logic of the pipe stage asynchronously.
    /// This method does not involve calling the next stage in the pipeline.
    /// </summary>
    /// <param name="instanceId">A unique identifier for the current pipe instance.</param>
    /// <param name="request">The request to process.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation, returning the response.</returns>
    protected abstract Task<TResponse?> ExecuteInternalAsync(Guid instanceId, TRequest request, CancellationToken cancellationToken);
    
    /// <summary>
    /// Executes the pipe stage asynchronously without a 'next' delegate.
    /// </summary>
    /// <param name="instanceId">A unique identifier for the current pipe instance.</param>
    /// <param name="request">The request to process.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation, returning the response.</returns>
    public async Task<TResponse?> ExecuteAsync(
        Guid instanceId, 
        TRequest request, 
        CancellationToken cancellationToken = default)
        => await ExecuteInternalAsync(instanceId, request, cancellationToken);
    
    /// <summary>
    /// Executes the pipe stage asynchronously with a 'next' delegate to pass control to the subsequent stage.
    /// </summary>
    /// <param name="instanceId">A unique identifier for the current pipe instance.</param>
    /// <param name="request">The request to process.</param>
    /// <param name="next">A delegate to invoke the next stage in the pipe.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation, returning the response.</returns>
    public async Task<TResponse?> ExecuteAsync(
        Guid instanceId, 
        TRequest request, 
        Func<Guid, TRequest, CancellationToken, Task<TResponse>> next, 
        CancellationToken cancellationToken = default)
    {
        var result = await ExecuteAsync(instanceId, request, cancellationToken);
        await next(instanceId, request, cancellationToken);
        return result;
    }
}