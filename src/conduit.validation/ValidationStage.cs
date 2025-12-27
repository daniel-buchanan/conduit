using conduit.logging;
using conduit.Pipes.Stages;
using Microsoft.Extensions.DependencyInjection;

namespace conduit.validation;

public class ValidationStage<TRequest, TResponse>(
    ILog logger,
    ConduitValidationConfiguration configuration,
    IServiceProvider provider) : PipeStage<TRequest, TResponse>(logger) 
    where TRequest : class, IRequest<TResponse> 
    where TResponse : class
{
    protected override async Task<StageResult<TRequest, TResponse>> ExecuteInternalAsync(Guid instanceId, TRequest request, CancellationToken cancellationToken)
    {
        var validator = provider.GetService<IModelValidator<TRequest>>();
        
        if(validator is null && configuration.ThrowOnValidatorNotFound)
            throw new ValidatorNotFoundException($"Validator not found for request type: {typeof(TRequest).Name}");

        if (validator is null) return StageResult.WithValidationResult<TRequest, TResponse>(ValidationResult.WithSuccess(request), this.GetType());
        var result = await validator.ValidateAsync(request);
        return StageResult.WithValidationResult<TRequest, TResponse>(result, this.GetType());
    }
}