namespace conduit.Pipes;

public record DebugResult<TResponse>(TResponse? Response, long OverallDurationMs, StageMetric[] Metrics)
{
    /// <summary>
    /// The overall duration of the pipe in milliseconds.
    /// </summary>
    public long OverallDurationMs { get; } = OverallDurationMs;
    
    /// <summary>
    /// The result of the pipe.
    /// </summary>
    public TResponse? Response { get; } = Response;
    
    /// <summary>
    /// Metrics about each stage in the process.
    /// </summary>
    public StageMetric[] Metrics { get; } = Metrics;
}