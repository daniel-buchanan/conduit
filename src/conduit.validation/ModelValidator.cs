using conduit.common;
using conduit.Pipes.Stages;
using conduit.validation.Rules;

namespace conduit.validation;

/// <summary>
/// Marker interface for all model validators in the Conduit system.
/// </summary>
public interface IModelValidator;

/// <summary>
/// Defines the contract for a model validator that validates a specific request type.
/// </summary>
/// <typeparam name="TRequest">The type of the request being validated.</typeparam>
/// <typeparam name="TResponse">The type of the response produced by the handler.</typeparam>
public interface IModelValidator<TRequest, TResponse> : IModelValidator
    where TRequest : class, IRequest<TResponse>
    where TResponse : class
{
    /// <summary>
    /// Synchronously validates the specified request.
    /// </summary>
    /// <param name="request">The request to validate.</param>
    /// <returns>A validation result containing any errors that occurred.</returns>
    ValidationResult<TRequest> Validate(TRequest request);
    
    /// <summary>
    /// Asynchronously validates the specified request.
    /// </summary>
    /// <param name="request">The request to validate.</param>
    /// <returns>A task that represents the asynchronous operation, returning a validation result.</returns>
    Task<ValidationResult<TRequest>> ValidateAsync(TRequest request);
}

/// <summary>
/// Provides an abstract base class for implementing model validators in the Conduit system.
/// </summary>
/// <typeparam name="TRequest">The type of the request being validated.</typeparam>
/// <typeparam name="TResponse">The type of the response produced by the handler.</typeparam>
public abstract class ModelValidator<TRequest, TResponse> : IModelValidator<TRequest, TResponse>
    where TRequest : class, IRequest<TResponse>
    where TResponse : class
{
    private readonly List<Rule<TRequest>> _rules = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="ModelValidator{TRequest, TResponse}"/> class.
    /// This constructor automatically configures the validator by calling <see cref="AddRules"/>.
    /// </summary>
    protected ModelValidator() => ConfigureSelf();

    /// <inheritdoc/>
    public ValidationResult<TRequest> Validate(TRequest request) 
        => ValidateAsync(request).Await();
    
    /// <inheritdoc/>
    public async Task<ValidationResult<TRequest>> ValidateAsync(TRequest request)
    {
        var isSuccess = true;
        var errors = new List<ValidationError>();
        foreach (var rule in _rules)
        {
            var result = await rule.ValidateAsync(request);
            isSuccess &= result.IsValid;
            if (result.Errors != null) errors.AddRange(result.Errors);
        }

        return isSuccess
            ? ValidationResult.WithSuccess(request)
            : ValidationResult.WithFailure(request, errors.ToArray());
    }

    private void ConfigureSelf()
    {
        var builder = new RuleBuilder<TRequest>();
        AddRules(builder);
        var rules = builder.Build();
        _rules.AddRange(rules);
    }

    /// <summary>
    /// When overridden in a derived class, configures the validation rules for this validator.
    /// </summary>
    /// <param name="ruleBuilder">The rule builder to use for configuring validation rules.</param>
    protected abstract Task AddRules(IRuleBuilder<TRequest> ruleBuilder);

    /// <summary>
    /// Helper method to select a property for validation configuration.
    /// </summary>
    /// <typeparam name="TProperty">The type of the property.</typeparam>
    /// <param name="prop">A function that selects the property from the request.</param>
    /// <returns>A rule builder for the selected property.</returns>
    protected IShouldBuilder<TRequest, TProperty> Property<TProperty>(Func<TRequest, TProperty> prop)
    {
        var builder = new RuleBuilder<TRequest>();
        return builder.Should(prop);
    }
}