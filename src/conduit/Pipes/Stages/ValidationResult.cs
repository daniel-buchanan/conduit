namespace conduit.Pipes.Stages;

/// <summary>
/// Represents a typed validation result for a specific request type.
/// </summary>
/// <typeparam name="TRequest">The type of the request being validated.</typeparam>
public class ValidationResult<TRequest> : ValidationResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationResult{TRequest}"/> class with a successful validation result.
    /// </summary>
    /// <param name="success">Whether the validation was successful.</param>
    /// <param name="request">The request that was validated.</param>
    public ValidationResult(bool success, TRequest request) : base(typeof(TRequest))
    {
        IsValid = success;
        Request = request;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationResult{TRequest}"/> class with validation errors.
    /// </summary>
    /// <param name="request">The request that was validated.</param>
    /// <param name="errors">The validation errors that occurred.</param>
    public ValidationResult(TRequest request, ValidationError[] errors) : base(typeof(TRequest))
    {
        IsValid = false;
        Request = request;
        Errors = errors;
    }
    
    /// <summary>
    /// Gets the request that was validated.
    /// </summary>
    public TRequest Request { get; }
}

/// <summary>
/// Represents a validation error for a specific property.
/// </summary>
/// <param name="PropertyName">The name of the property that failed validation.</param>
/// <param name="Message">The validation error message.</param>
public record ValidationError(string PropertyName, string? Message)
{
    /// <summary>
    /// Gets the name of the property that failed validation.
    /// </summary>
    public string PropertyName { get; } = PropertyName;
    
    /// <summary>
    /// Gets the validation error message.
    /// </summary>
    public string? Message { get; } = Message;
}

/// <summary>
/// Represents the base validation result containing information about validation success and errors.
/// </summary>
/// <param name="modelType">The type of the model that was validated.</param>
public class ValidationResult(Type modelType)
{
    /// <summary>
    /// Gets the type of the model that was validated.
    /// </summary>
    public Type ModelType { get; init; } = modelType;
    
    /// <summary>
    /// Gets a value indicating whether the validation was successful.
    /// </summary>
    public bool IsValid { get; protected init; }
    
    /// <summary>
    /// Gets the validation errors that occurred, if any.
    /// </summary>
    public ValidationError[]? Errors { get; protected init; }
    
    /// <summary>
    /// Creates a successful validation result for the specified request.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request.</typeparam>
    /// <param name="model">The request that was validated successfully.</param>
    /// <returns>A successful validation result.</returns>
    public static ValidationResult<TRequest> WithSuccess<TRequest>(TRequest model) => new(true, model);
    
    /// <summary>
    /// Creates a validation result with errors for the specified request.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request.</typeparam>
    /// <param name="model">The request that failed validation.</param>
    /// <param name="errors">The validation errors that occurred.</param>
    /// <returns>A validation result with errors.</returns>
    public static ValidationResult<TRequest> WithFailure<TRequest>(TRequest model, ValidationError[] errors) => new(model, errors);
}