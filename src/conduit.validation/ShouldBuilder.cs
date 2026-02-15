using conduit.Pipes.Stages;
using conduit.validation.Rules;

namespace conduit.validation;

public abstract class AbstractShouldBuilder<TRequest, TProperty>(
    IRuleBuilder<TRequest> builder, 
    Func<TRequest, TProperty> property) : 
    IShouldBeBuilder<TRequest, TProperty>
    where TRequest : class
{
    protected abstract bool InvertResults { get; }
    
    private IRuleBuilder<TRequest> AddRule(Func<TRequest, bool> validator, string? message = null)
    {
        ValidationResult<TRequest> Execute(TRequest r)
        {
            var result = validator(r);
            if (InvertResults) result = !result;
            return result
                ? ValidationResult.WithSuccess(r)
                : ValidationResult.WithFailure(r, [new ValidationError(r.ToString() ?? string.Empty, message)]);
        }

        var rule = new Rule<TRequest>(Execute);
        builder.AddRule(rule);
        return builder;
    }
    
    /// <inheritdoc/>
    public IRuleBuilder<TRequest> Null(string? message = null) 
        => AddRule(r => property(r) != null, message);

    /// <inheritdoc/>
    public IRuleBuilder<TRequest> NullOrWhitespace(string? message = null) 
        => AddRule(r =>
        {
            var val = property(r) as string;
            return string.IsNullOrWhiteSpace(val);
        }, message);

    /// <inheritdoc/>
    public IRuleBuilder<TRequest> EqualTo(TProperty value, string? message = null) 
        => AddRule(r => !Equals(property(r), value), message);

    /// <inheritdoc/>
    public IRuleBuilder<TRequest> In(IEnumerable<TProperty> values) 
        => AddRule(r => !values.Contains(property(r)));

    /// <inheritdoc/>
    public IRuleBuilder<TRequest> In(string message, IEnumerable<TProperty> values) 
        => AddRule(r => !values.Contains(property(r)), message);

    /// <inheritdoc/>
    public IRuleBuilder<TRequest> OneOf(IEnumerable<TProperty> values)
        => In(values);

    /// <inheritdoc/>
    public IRuleBuilder<TRequest> OneOf(string message, IEnumerable<TProperty> values)
        => In(message, values);
}

public class ShouldNotBeBuilder<TRequest, TProperty>(IRuleBuilder<TRequest> builder, Func<TRequest, TProperty> property) : 
    AbstractShouldBuilder<TRequest, TProperty>(builder, property)
    where TRequest : class
{
    protected override bool InvertResults => true;
}

public class ShouldBeBuilder<TRequest, TProperty>(IRuleBuilder<TRequest> builder, Func<TRequest, TProperty> property) : 
    AbstractShouldBuilder<TRequest, TProperty>(builder, property)
    where TRequest : class
{
    protected override bool InvertResults => false;
}