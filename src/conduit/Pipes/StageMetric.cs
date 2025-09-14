namespace conduit.Pipes;

/// <summary>
/// Represents metrics collected for a single stage within a Conduit pipe.
/// </summary>
/// <param name="Index">The zero-based index of the stage in the pipeline.</param>
/// <param name="Name">The name of the stage.</param>
/// <param name="Duration">The duration of the stage execution in milliseconds.</param>
/// <param name="Success">A boolean indicating whether the stage executed successfully.</param>
/// <param name="Exception">An optional exception if the stage failed.</param>
public record StageMetric(int Index, string Name, long Duration, bool Success, Exception? Exception = null)
{
    /// <summary>
    /// Gets the zero-based index of the stage in the pipeline.
    /// </summary>
    public int Index { get; } = Index;
    /// <summary>
    /// Gets the name of the stage.
    /// </summary>
    public string Name { get; } = Name;
    /// <summary>
    /// Gets the duration of the stage execution in milliseconds.
    /// </summary>
    public long Duration { get; } = Duration;
    /// <summary>
    /// Gets a value indicating whether the stage executed successfully.
    /// </summary>
    public bool Success { get; } = Success;
    /// <summary>
    /// Gets the exception that occurred during stage execution, if any.
    /// </summary>
    public Exception? Exception { get; } = Exception;
}