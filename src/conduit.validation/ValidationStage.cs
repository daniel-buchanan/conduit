using conduit.logging;
using conduit.Pipes;
using conduit.Pipes.Stages;
using Microsoft.Extensions.DependencyInjection;

namespace conduit.validation;

/// <summary>
/// Represents a pipeline stage that performs model validation on incoming requests.
/// </summary>
/// <typeparam name="TRequest">The type of the request being validated.</typeparam>
/// <typeparam name="TResponse">The type of the response to be produced.</typeparam>
/// <param name="logger">The logger instance to use for this stage.</param>
/// <param name="configuration">The validation configuration.</param>
/// <param name="provider">The service provider for resolving validators.</param>
public class ValidationStage<TRequest, TResponse>(
    ILog logger,
    ConduitValidationConfiguration configuration,
    IServiceProvider provider) : PipeStage<TRequest, TResponse>(logger), IValidationPipeStage
    where TRequest : class, IRequest<TResponse>
    where TResponse : class
{
    /// <inheritdoc/>
    protected override async Task<StageResult<TRequest, TResponse>> ExecuteInternalAsync(Guid instanceId, TRequest request, CancellationToken cancellationToken)
    {
        var validator = provider.GetService<IModelValidator<TRequest, TResponse>>();
        
        if(validator is null && configuration.ThrowOnValidatorNotFound)
            throw new ValidatorNotFoundException($"Validator not found for request type: {typeof(TRequest).Name}");

        if (validator is null) return StageResult.WithValidationResult<TRequest, TResponse>(ValidationResult.WithSuccess(request), this.GetType());
        var result = await validator.ValidateAsync(request);
        return StageResult.WithValidationResult<TRequest, TResponse>(result, this.GetType());
    }
}