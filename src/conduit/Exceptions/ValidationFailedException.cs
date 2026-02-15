using conduit.Pipes.Stages;

namespace conduit.Exceptions;

/// <summary>
/// The exception that is thrown when model validation fails during pipeline execution.
/// </summary>
public class ValidationFailedException(ValidationResult validationResult) : Exception(GetMessage(validationResult))
{
    /// <summary>
    /// Gets the validation errors that caused this exception.
    /// </summary>
    public ValidationError[] ValidationErrors { get; } = validationResult.Errors!;

    private static string GetMessage(ValidationResult result) => $"Validation Failed for {result.ModelType.Name}";
}