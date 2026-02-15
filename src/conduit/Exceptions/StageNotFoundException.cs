namespace conduit.Exceptions;

/// <summary>
/// The exception that is thrown when a required pipeline stage cannot be found or resolved.
/// </summary>
public class StageNotFoundException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StageNotFoundException"/> class with a default message.
    /// </summary>
    public StageNotFoundException() : base("Pipe not found") { }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="StageNotFoundException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    public StageNotFoundException(string message) : base(message) { }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="StageNotFoundException"/> class with a specified error message and an inner exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="inner">The exception that is the cause of the current exception.</param>
    public StageNotFoundException(string message, Exception inner) : base(message, inner) { }
}