using conduit.Pipes.Stages;

namespace conduit.Pipes;

/// <summary>
/// Represents the default pipe implementation in the Conduit system.
/// This pipe orchestrates the execution of pre-execution, request handling, and post-execution stages.
/// </summary>
/// <typeparam name="TRequest">The type of the request handled by this pipe.</typeparam>
/// <typeparam name="TResponse">The type of the response produced by this pipe.</typeparam>
/// <param name="pre">The debug pre-execution stage.</param>
/// <param name="handler">The stage responsible for handling the main request.</param>
/// <param name="post">The debug post-execution stage.</param>
public class DefaultPipe<TRequest, TResponse>(
    DebugPreExecutionStage<TRequest, TResponse> pre,
    HandleRequestStage<TRequest, TResponse> handler,
    DebugPostExecutionStage<TRequest, TResponse> post)
    : Pipe<TRequest, TResponse>
    where TRequest : class, IRequest<TResponse>
    where TResponse : class 
{
    /// <summary>
    /// Sends a request through the default pipe, executing the pre-execution, handler, and post-execution stages.
    /// </summary>
    /// <param name="request">The request to send.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation, returning the response from the handler.</returns>
    public override async Task<TResponse?> SendAsync(TRequest request, CancellationToken cancellationToken = default)
    {
        var instanceId = Guid.NewGuid();
        await pre.ExecuteAsync(instanceId, request, cancellationToken);
        var response = await handler.ExecuteAsync(instanceId, request, cancellationToken);
        await post.ExecuteAsync(instanceId, request, cancellationToken);
        return response;
    }
}