namespace conduit.validation;

public record ConduitValidationConfiguration(bool ThrowOnValidatorNotFound)
{
    public bool ThrowOnValidatorNotFound { get; } = ThrowOnValidatorNotFound;
}