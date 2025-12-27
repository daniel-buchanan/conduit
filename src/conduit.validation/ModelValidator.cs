using conduit.Pipes.Stages;

namespace conduit.validation;

public interface IModelValidator;

public interface IModelValidator<TRequest> : IModelValidator
{
    ValidationResult<TRequest> Validate(TRequest request);
    Task<ValidationResult<TRequest>> ValidateAsync(TRequest request);
}

public abstract class ModelValidator<TRequest> : IModelValidator<TRequest> where TRequest : class
{
    private readonly List<Rule<TRequest>> _rules = new();

    protected ModelValidator() => ConfigureSelf();

    public ValidationResult<TRequest> Validate(TRequest request)
    {
        var t = ValidateAsync(request);
        t.Wait();
        return t.Result;
    }

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
}