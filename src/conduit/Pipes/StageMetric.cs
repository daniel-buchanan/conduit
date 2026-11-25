namespace conduit.Pipes;

/// <summary>
/// Represents metrics collected for a single stage within a Conduit pipe.
/// </summary>
/// <param name="Index">The zero-based index of the stage in the pipe.</param>
/// <param name="Name">The name of the stage.</param>
/// <param name="PrefetchDuration">The duration of the prefetch.</param>
/// <param name="Duration">The duration of the stage execution in milliseconds.</param>
public record StageMetric(int Index, string Name, long PrefetchDuration, long Duration)
{
    /// <summary>
    /// Gets the zero-based index of the stage in the pipe.
    /// </summary>
    public int Index { get; } = Index;
    
    /// <summary>
    /// Gets the name of the stage.
    /// </summary>
    public string Name { get; } = Name;
    
    public long PrefetchDuration { get; } = PrefetchDuration;
    
    /// <summary>
    /// Gets the duration of the stage execution in milliseconds.
    /// </summary>
    public long Duration { get; } = Duration;
}