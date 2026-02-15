namespace conduit.common;

/// <summary>
/// Defines the logging levels supported by the Conduit logging system.
/// </summary>
public enum LoggingLevel
{
    /// <summary>
    /// Debug level - detailed diagnostic information.
    /// </summary>
    Debug = 0,
    
    /// <summary>
    /// Verbose level - verbose logging information.
    /// </summary>
    Verbose = 1,
    
    /// <summary>
    /// Info level - general informational messages.
    /// </summary>
    Info = 2,
    
    /// <summary>
    /// Warning level - warning messages for potentially harmful situations.
    /// </summary>
    Warning = 4,
    
    /// <summary>
    /// Error level - error messages for serious problems.
    /// </summary>
    Error = 8
}