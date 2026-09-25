namespace conduit.validation;

/// <summary>
/// The exception that is thrown when <c>AddValidation</c> is called more than once on the same
/// <see cref="IConduitConfigurationBuilder"/>. Each call builds its own <see cref="ValidationBuilder"/>, so
/// a second call's scan-collision detection and configuration (e.g. <see cref="IValidationBuilder.ThrowIfValidatorNotFound"/>)
/// would silently start over instead of merging with the first call's — see ADR-0011.
/// </summary>
public class ValidationAlreadyConfiguredException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationAlreadyConfiguredException"/> class with a default message.
    /// </summary>
    public ValidationAlreadyConfiguredException() : base("Validation has already been configured for this Conduit configuration.") { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationAlreadyConfiguredException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    public ValidationAlreadyConfiguredException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationAlreadyConfiguredException"/> class with a specified error message and an inner exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="inner">The exception that is the cause of the current exception.</param>
    public ValidationAlreadyConfiguredException(string message, Exception inner) : base(message, inner) { }
}
