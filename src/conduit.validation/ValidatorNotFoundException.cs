namespace conduit.validation;

/// <summary>
/// The exception that is thrown when a required validator cannot be found for a request type.
/// </summary>
public class ValidatorNotFoundException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValidatorNotFoundException"/> class with a specified error message.
    /// </summary>
    /// <param name="msg">The error message that explains the reason for the exception.</param>
    public ValidatorNotFoundException(string msg) : base(msg) { }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="ValidatorNotFoundException"/> class with a specified error message and an inner exception.
    /// </summary>
    /// <param name="msg">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ValidatorNotFoundException(string msg, Exception innerException) : base(msg, innerException) { }
}