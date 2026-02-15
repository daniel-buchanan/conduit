using conduit.common;
using conduit.Pipes.Stages;
using conduit.validation.Rules;

namespace conduit.validation;

public interface IModelValidator;

public interface IModelValidator<TRequest, TResponse> : IModelValidator
    where TRequest : class, IRequest<TResponse>
    where TResponse : class
{
    ValidationResult<TRequest> Validate(TRequest request);
    Task<ValidationResult<TRequest>> ValidateAsync(TRequest request);
}

public abstract class ModelValidator<TRequest, TResponse> : IModelValidator<TRequest, TResponse>
    where TRequest : class, IRequest<TResponse>
    where TResponse : class
{
    private readonly List<Rule<TRequest>> _rules = [];

    protected ModelValidator() => ConfigureSelf();

    public ValidationResult<TRequest> Validate(TRequest request) 
        => ValidateAsync(request).Await();
    
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

    protected abstract Task AddRules(IRuleBuilder<TRequest> ruleBuilder);

    protected IRuleBuilder<TRequest> Property<TProperty>(Func<TRequest, TProperty> prop)
    {
        throw new NotImplementedException();
    }
}