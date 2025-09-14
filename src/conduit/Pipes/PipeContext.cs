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
    /// <summary>
    /// Gets the request associated with this pipe context.
    /// </summary>
    public IRequest<TResponse> Request { get; } = request;
    /// <summary>
    /// Gets the response associated with this pipe context.
    /// </summary>
    public TResponse? Response { get; private set; }
    /// <summary>
    /// Gets the total number of stages configured for the current pipe.
    /// </summary>
    public int CountOfStages { get; } = countOfStages;
    /// <summary>
    /// Gets an array of metrics collected for each stage in the pipe.
    /// </summary>
    public StageMetric[] Metrics { get; } = new StageMetric[countOfStages];
    
    /// <summary>
    /// Sets the response for the current pipe context.
    /// </summary>
    /// <param name="response">The response to set.</param>
    public void SetResponse(TResponse? response)
        => Response = response;

    /// <summary>
    /// Sets the metric for a specific stage in the pipe.
    /// </summary>
    /// <param name="index">The index of the stage.</param>
    /// <param name="name">The name of the stage.</param>
    /// <param name="duration">The duration of the stage execution in milliseconds.</param>
    /// <param name="success">A boolean indicating whether the stage executed successfully.</param>
    /// <param name="exception">An optional exception if the stage failed.</param>
    public void SetStageMetric(int index, string name, long duration, bool success, Exception? exception = null)
    {
        Metrics[index] = new StageMetric(index, name, duration, success, exception);
    }
}