namespace conduit.Pipes;

public record DebugResult<TResponse>(TResponse? Response, long OverallDurationMs, StageMetric[] Metrics, bool ShortCircuited = false)
{
    /// <summary>
    /// The overall duration of the pipeline in milliseconds.
    /// </summary>
    public long OverallDurationMs { get; } = OverallDurationMs;

    /// <summary>
    /// The result of the pipeline.
    /// </summary>
    public TResponse? Response { get; } = Response;

    /// <summary>
    /// Metrics about each stage in the process.
    /// </summary>
    public StageMetric[] Metrics { get; } = Metrics;

    /// <summary>
    /// Gets whether the pipe stopped early because a registered exception handler called
    /// <c>RequestExceptionHandlerState&lt;TResponse&gt;.SetHandled</c>. When <c>true</c>, <see cref="Metrics"/>
    /// only covers the stages that actually ran.
    /// </summary>
    public bool ShortCircuited { get; } = ShortCircuited;
}