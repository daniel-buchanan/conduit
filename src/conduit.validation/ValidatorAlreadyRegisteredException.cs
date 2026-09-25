namespace conduit.validation;

/// <summary>
/// The exception that is thrown when an assembly scan discovers two or more validators for the same
/// (TRequest, TResponse) pair. Mirrors <c>PipeAlreadyRegisteredException</c>. Does not apply to explicit
/// <see cref="IValidationBuilder.WithValidatorFor{TRequest,TResponse}(IModelValidator{TRequest,TResponse})"/>
/// registrations, which are always allowed to override a scanned entry.
/// </summary>
public class ValidatorAlreadyRegisteredException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValidatorAlreadyRegisteredException"/> class with a default message.
    /// </summary>
    public ValidatorAlreadyRegisteredException() : base("Validator already Registered") { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidatorAlreadyRegisteredException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    public ValidatorAlreadyRegisteredException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidatorAlreadyRegisteredException"/> class with a specified error message and an inner exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="inner">The exception that is the cause of the current exception.</param>
    public ValidatorAlreadyRegisteredException(string message, Exception inner) : base(message, inner) { }
}
