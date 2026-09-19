namespace conduit;

/// <summary>
/// Defines the contract for configuring a single <c>RegisterHandler</c> registration.
/// </summary>
public interface IHandlerRegistrationOptions
{
    /// <summary>
    /// Excludes this pipe from any default stage that implements <c>IValidationPipeStage</c>
    /// (e.g. the validation stage registered by the conduit.validation package).
    /// </summary>
    /// <returns>The current options instance.</returns>
    IHandlerRegistrationOptions ExcludeValidation();
}
