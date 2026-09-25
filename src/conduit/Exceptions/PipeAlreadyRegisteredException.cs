namespace conduit.Exceptions;

/// <summary>
/// The exception that is thrown when attempting to register a pipe that has already been registered in the Conduit system.
/// </summary>
public class PipeAlreadyRegisteredException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PipeAlreadyRegisteredException"/> class with a default message.
    /// </summary>
    public PipeAlreadyRegisteredException() : base("Pipe already Registered") { }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="PipeAlreadyRegisteredException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    public PipeAlreadyRegisteredException(string message) : base(message) { }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="PipeAlreadyRegisteredException"/> class with a specified error message and an inner exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="inner">The exception that is the cause of the current exception.</param>
    public PipeAlreadyRegisteredException(string message, Exception inner) : base(message, inner) { }
}