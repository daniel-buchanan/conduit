namespace conduit.Configuration;

/// <summary>
/// Provides a concrete implementation of <see cref="IHandlerRegistrationOptions"/>.
/// </summary>
public class HandlerRegistrationOptions : IHandlerRegistrationOptions
{
    /// <summary>
    /// Gets a value indicating whether this registration is excluded from default validation.
    /// </summary>
    public bool IsValidationExcluded { get; private set; }

    /// <inheritdoc />
    public IHandlerRegistrationOptions ExcludeValidation()
    {
        IsValidationExcluded = true;
        return this;
    }
}
