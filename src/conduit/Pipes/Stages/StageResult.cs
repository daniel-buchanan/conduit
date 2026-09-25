namespace conduit.Pipes.Stages;

/// <summary>
/// Represents the result of executing a pipeline stage, including the response and any validation errors or exceptions.
/// </summary>
/// <typeparam name="TRequest">The type of the request being processed.</typeparam>
/// <typeparam name="TResponse">The type of the response produced by the stage.</typeparam>
public class StageResult<TRequest, TResponse>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StageResult{TRequest, TResponse}"/> class with a successful result.
    /// </summary>
    /// <param name="response">The response produced by the stage.</param>
    /// <param name="stageType">The type of the stage that produced this result.</param>
    public StageResult(TResponse? response, Type stageType)
    {
        IsSuccessful = true;
        Result = response;
        ValidationErrors = [];
        Exception = null;
        StageType = stageType;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StageResult{TRequest, TResponse}"/> class with an indeterminate result.
    /// </summary>
    /// <param name="stageType">The type of the stage that produced this result.</param>
    /// <param name="_">An indeterminate result marker.</param>
    public StageResult(Type stageType, IndeterminateResult _)
    {
        IsSuccessful = true;
        IsIndeterminate = true;
        ValidationErrors = [];
        Exception = null;
        StageType = stageType;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StageResult{TRequest, TResponse}"/> class with validation results.
    /// </summary>
    /// <param name="result">The validation result.</param>
    /// <param name="stageType">The type of the stage that produced this result.</param>
    public StageResult(ValidationResult<TRequest> result, Type stageType)
    {
        IsSuccessful = result.IsValid;
        IsIndeterminate = false;
        ValidationErrors = result.Errors ?? [];
        Exception = null;
        StageType = stageType;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StageResult{TRequest, TResponse}"/> class with an exception.
    /// </summary>
    /// <param name="exception">The exception that occurred during stage execution.</param>
    /// <param name="stageType">The type of the stage that produced this result.</param>
    public StageResult(Exception exception, Type stageType)
    {
        Result = default;
        IsSuccessful = false;
        IsIndeterminate = false;
        ValidationErrors = [];
        Exception = exception;
        StageType = stageType;
    }

    /// <summary>
    /// Gets the type of the stage that produced this result.
    /// </summary>
    public Type StageType { get; }
    
    /// <summary>
    /// Gets the response produced by the stage.
    /// </summary>
    public TResponse? Result { get; }
    
    /// <summary>
    /// Gets a value indicating whether this stage produced an indeterminate result (neither success nor failure).
    /// </summary>
    public bool IsIndeterminate { get; }
    
    /// <summary>
    /// Gets a value indicating whether the stage execution was successful.
    /// </summary>
    public bool IsSuccessful { get; }
    
    /// <summary>
    /// Gets the validation errors that occurred, if any.
    /// </summary>
    public ValidationError[] ValidationErrors { get; }
    
    /// <summary>
    /// Gets the exception that occurred during stage execution, if any.
    /// </summary>
    public Exception? Exception { get; }
}

/// <summary>
/// Provides helper methods for creating stage results.
/// </summary>
public static class StageResult
{
    /// <summary>
    /// Creates a successful stage result with the specified response.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request.</typeparam>
    /// <typeparam name="TResponse">The type of the response.</typeparam>
    /// <param name="response">The response produced by the stage.</param>
    /// <param name="stageType">The type of the stage.</param>
    /// <returns>A successful stage result.</returns>
    public static StageResult<TRequest, TResponse> WithResult<TRequest, TResponse>(TResponse? response, Type stageType) 
        => new(response, stageType);
    
    /// <summary>
    /// Creates an indeterminate stage result.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request.</typeparam>
    /// <typeparam name="TResponse">The type of the response.</typeparam>
    /// <param name="stageType">The type of the stage.</param>
    /// <returns>An indeterminate stage result.</returns>
    public static StageResult<TRequest, TResponse> WithIndeterminateResult<TRequest, TResponse>(Type stageType)
        => new(stageType, new IndeterminateResult());
    
    /// <summary>
    /// Creates a validation result.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request.</typeparam>
    /// <typeparam name="TResponse">The type of the response.</typeparam>
    /// <param name="result">The validation result.</param>
    /// <param name="stageType">The type of the stage.</param>
    /// <returns>A stage result with validation information.</returns>
    public static StageResult<TRequest, TResponse> WithValidationResult<TRequest, TResponse>(ValidationResult<TRequest> result,  Type stageType)
        => new(result, stageType);
    
    /// <summary>
    /// Creates a failed stage result with an exception.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request.</typeparam>
    /// <typeparam name="TResponse">The type of the response.</typeparam>
    /// <param name="exception">The exception that occurred.</param>
    /// <param name="stageType">The type of the stage.</param>
    /// <returns>A failed stage result.</returns>
    public static StageResult<TRequest, TResponse> WithException<TRequest, TResponse>(Exception exception, Type stageType)
        => new(exception, stageType);
}
