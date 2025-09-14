namespace conduit.Pipes;

/// <summary>
/// Defines the contract for the context of a Conduit pipe, carrying request, response, and metrics through the pipeline.
/// </summary>
/// <typeparam name="T">The type of the response for the current pipe context.</typeparam>
public interface IPipeContext<T>
    where T: class
{
    /// <summary>
    /// Gets the request associated with this pipe context.
    /// </summary>
    IRequest<T> Request { get; }
    /// <summary>
    /// Gets the response associated with this pipe context.
    /// </summary>
    T? Response { get; }
    /// <summary>
    /// Gets the total number of stages configured for the current pipe.
    /// </summary>
    int CountOfStages { get; }
    /// <summary>
    /// Gets an array of metrics collected for each stage in the pipe.
    /// </summary>
    StageMetric[] Metrics { get; }
    /// <summary>
    /// Sets the response for the current pipe context.
    /// </summary>
    /// <param name="response">The response to set.</param>
    void SetResponse(T? response);
    /// <summary>
    /// Sets the metric for a specific stage in the pipe.
    /// </summary>
    /// <param name="index">The index of the stage.</param>
    /// <param name="name">The name of the stage.</param>
    /// <param name="duration">The duration of the stage execution in milliseconds.</param>
    /// <param name="success">A boolean indicating whether the stage executed successfully.</param>
    /// <param name="exception">An optional exception if the stage failed.</param>
    void SetStageMetric(int index, string name, long duration, bool success, Exception? exception = null);
}