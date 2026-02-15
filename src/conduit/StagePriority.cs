namespace conduit;

/// <summary>
/// Defines the execution priority levels for pipeline stages in the Conduit system.
/// </summary>
public enum StagePriority
{
    /// <summary>
    /// The stage should execute first in the pipeline.
    /// </summary>
    First = 0,
    
    /// <summary>
    /// The stage has no specific priority and will execute in standard order.
    /// </summary>
    Indifferent = 1,
    
    /// <summary>
    /// The stage should execute last in the pipeline.
    /// </summary>
    Last = Int32.MaxValue
}