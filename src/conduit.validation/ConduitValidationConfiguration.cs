namespace conduit.validation;

/// <summary>
/// Represents the configuration settings for Conduit validation.
/// </summary>
/// <param name="ThrowOnValidatorNotFound">Whether to throw an exception if a validator cannot be found for a request type.</param>
public record ConduitValidationConfiguration(bool ThrowOnValidatorNotFound)
{
    /// <summary>
    /// Gets a value indicating whether to throw an exception if a validator is not found for a request type.
    /// </summary>
    public bool ThrowOnValidatorNotFound { get; } = ThrowOnValidatorNotFound;
}