using conduit.logging;

namespace conduit.Pipes.Stages;

/// <summary>
/// Represents a debug pipe stage that executes after the main request handler.
/// This stage logs a debug message indicating post-execution.
/// </summary>
/// <typeparam name="TRequest">The type of the request handled by this stage.</typeparam>
/// <typeparam name="TResponse">The type of the response produced by this stage.</typeparam>
/// <param name="logger">The logger instance to be used by the stage.</param>
public class DebugPostExecutionStage<TRequest, TResponse>(ILog logger) : PipeStage<TRequest, TResponse>(logger) 
    where TRequest : class, IRequest<TResponse>
    where TResponse : class
{
    /// <summary>
    /// Executes the debug post-execution logic, logging a message.
    /// </summary>
    /// <param name="instanceId">A unique identifier for the current pipe instance.</param>
    /// <param name="request">The request being processed.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation, returning a default response.</returns>
    protected override Task<TResponse?> ExecuteInternalAsync(Guid instanceId, TRequest request, CancellationToken cancellationToken)
    {
        Logger.Debug("[POST] ICall<{0}>.ExecuteAsync", typeof(TRequest).Name);
        return Task.FromResult(default(TResponse));
    }
}