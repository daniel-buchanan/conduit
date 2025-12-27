namespace conduit.Pipes.Stages;

public class StageResult<TRequest, TResponse>
{
    public StageResult(TResponse? response, Type stageType)
    {
        IsSuccessful = true;
        Result = response;
        ValidationErrors = [];
        Exception = null;
        StageType = stageType;
    }

    public StageResult(Type stageType, IndeterminateResult _)
    {
        IsSuccessful = true;
        IsIndeterminate = true;
        ValidationErrors = [];
        Exception = null;
        StageType = stageType;
    }

    public StageResult(ValidationResult<TRequest> result, Type stageType)
    {
        IsSuccessful = result.IsValid;
        IsIndeterminate = false;
        ValidationErrors = result.Errors ?? [];
        Exception = null;
        StageType = stageType;
    }

    public StageResult(Exception exception, Type stageType)
    {
        Result = default;
        IsSuccessful = false;
        IsIndeterminate = false;
        ValidationErrors = [];
        Exception = exception;
        StageType = stageType;
    }

    public Type StageType { get; }
    
    public TResponse? Result { get; }
    
    public bool IsIndeterminate { get; }
    
    public bool IsSuccessful { get; }
    
    public ValidationError[] ValidationErrors { get; }
    
    public Exception? Exception { get; }
}

public static class StageResult
{
    public static StageResult<TRequest, TResponse> WithResult<TRequest, TResponse>(TResponse? response, Type stageType) 
        => new(response, stageType);
    
    public static StageResult<TRequest, TResponse> WithIndeterminateResult<TRequest, TResponse>(Type stageType)
        => new(stageType, new IndeterminateResult());
    
    public static StageResult<TRequest, TResponse> WithValidationResult<TRequest, TResponse>(ValidationResult<TRequest> result,  Type stageType)
        => new(result, stageType);
    
    public static StageResult<TRequest, TResponse> WithException<TRequest, TResponse>(Exception exception, Type stageType)
        => new(exception, stageType);
}
