namespace conduit.Exceptions;

/// <summary>
/// The exception that is thrown when a required pipe cannot be found in the Conduit system.
/// </summary>
public class PipeNotFoundException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PipeNotFoundException"/> class with a default message.
    /// </summary>
    public PipeNotFoundException() : base("Pipe not found") { }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="PipeNotFoundException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    public PipeNotFoundException(string message) : base(message) { }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="PipeNotFoundException"/> class with a specified error message and an inner exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="inner">The exception that is the cause of the current exception.</param>
    public PipeNotFoundException(string message, Exception inner) : base(message, inner) { }
}