namespace conduit.Pipes.Stages;

public class ValidationResult<TRequest> : ValidationResult
{
    public ValidationResult(bool success, TRequest request) : base(typeof(TRequest))
    {
        IsValid = success;
        Request = request;
    }

    public ValidationResult(TRequest request, ValidationError[] errors) : base(typeof(TRequest))
    {
        IsValid = false;
        Request = request;
        Errors = errors;
    }
    
    public TRequest Request { get; }
}

public record ValidationError(string PropertyName, string? Message)
{
    public string PropertyName { get; } = PropertyName;
    public string? Message { get; } = Message;
}

public class ValidationResult(Type modelType)
{
    public Type ModelType { get; init; } = modelType;
    
    public bool IsValid { get; protected init; }
    
    public ValidationError[]? Errors { get; protected init; }
    
    public static ValidationResult<TRequest> WithSuccess<TRequest>(TRequest model) => new(true, model);
    
    public static ValidationResult<TRequest> WithFailure<TRequest>(TRequest model, ValidationError[] errors) => new(model, errors);
}