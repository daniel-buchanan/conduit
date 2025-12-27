namespace conduit.Pipes;

/// <summary>
/// Represents the default implementation of <see cref="IPipeContext{TResponse}"/>,
/// holding the state and metrics for a request as it flows through the Conduit pipeline.
/// </summary>
/// <typeparam name="TResponse">The type of the response for the current pipe context.</typeparam>
/// <param name="countOfStages">The total number of stages in the pipe.</param>
/// <param name="request">The original request.</param>
public class PipeContext<TResponse>(int countOfStages, IRequest<TResponse> request) : IPipeContext<TResponse> 
    where TResponse : class
{
    /// <inheritdoc/>
    public IRequest<TResponse> Request { get; } = request;
    
    /// <inheritdoc/>
    public TResponse? Response { get; private set; }
    
    /// <inheritdoc/>
    public int CountOfStages { get; } = countOfStages;
    
    /// <inheritdoc/>
    public StageMetric[] Metrics { get; } = new StageMetric[countOfStages];
    
    /// <inheritdoc/>
    public void SetResponse(TResponse? response)
        => Response = response;

    /// <inheritdoc/>
    public void SetStageMetric(
        int index,
        string name, 
        long prefetchDuration,
        long duration,
        bool success,
        Exception? exception = null) 
        => Metrics[index] = new StageMetric(index, name, prefetchDuration, duration, success, exception);
}