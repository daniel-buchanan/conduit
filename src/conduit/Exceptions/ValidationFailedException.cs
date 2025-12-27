using conduit.Pipes.Stages;

namespace conduit.Exceptions;

public class ValidationFailedException(ValidationResult validationResult) : Exception(GetMessage(validationResult))
{
    public ValidationError[] ValidationErrors { get; } = validationResult.Errors!;

    private static string GetMessage(ValidationResult result) => $"Validation Failed for {result.ModelType.Name}";
}