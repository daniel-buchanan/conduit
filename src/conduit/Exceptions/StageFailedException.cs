namespace conduit.Exceptions;

/// <summary>
/// The exception that is thrown when a pipeline stage fails during execution.
/// </summary>
public class StageFailedException : Exception, IPassthroughException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StageFailedException"/> class with a default message.
    /// </summary>
    public StageFailedException() : base("Stage Failed during Execution") { }

    /// <summary>
    /// Initializes a new instance of the <see cref="StageFailedException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    public StageFailedException(string message) : base(message) { }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="StageFailedException"/> class with a specified error message and an inner exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="inner">The exception that is the cause of the current exception.</param>
    public StageFailedException(string message, Exception inner) : base(message, inner) { }
}