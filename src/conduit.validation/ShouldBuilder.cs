using conduit.Pipes.Stages;
using conduit.validation.Rules;

namespace conduit.validation;

/// <summary>
/// Provides an abstract base class for building validation conditions for properties.
/// </summary>
/// <typeparam name="TRequest">The type of request being validated.</typeparam>
/// <typeparam name="TProperty">The type of the property being validated.</typeparam>
public abstract class AbstractShouldBuilder<TRequest, TProperty>(
    IRuleBuilder<TRequest> builder,
    Func<TRequest, TProperty> property,
    string propertyName) :
    IShouldBeBuilder<TRequest, TProperty>
    where TRequest : class
{
    /// <summary>
    /// Gets a value indicating whether the validation results should be inverted.
    /// </summary>
    protected abstract bool InvertResults { get; }

    /// <summary>
    /// Gets the function that selects the property being validated.
    /// </summary>
    protected Func<TRequest, TProperty> Property => property;

    /// <summary>
    /// Adds a rule to the underlying builder, applying <see cref="InvertResults"/> to the validator's outcome.
    /// </summary>
    /// <param name="validator">A function that returns whether the property is valid.</param>
    /// <param name="message">An optional custom error message.</param>
    protected IRuleBuilder<TRequest> AddRule(Func<TRequest, bool> validator, string? message = null)
    {
        ValidationResult<TRequest> Execute(TRequest r)
        {
            var result = validator(r);
            if (InvertResults) result = !result;
            return result
                ? ValidationResult.WithSuccess(r)
                : ValidationResult.WithFailure(r, [new ValidationError(propertyName, message)]);
        }

        var rule = new Rule<TRequest>(Execute);
        builder.AddRule(rule);
        return builder;
    }

    /// <inheritdoc/>
    public IRuleBuilder<TRequest> Null(string? message = null)
        => AddRule(r => property(r) is null, message);

    /// <inheritdoc/>
    public IRuleBuilder<TRequest> EqualTo(TProperty value, string? message = null)
        => AddRule(r => Equals(property(r), value), message);

    /// <inheritdoc/>
    public IRuleBuilder<TRequest> In(IEnumerable<TProperty> values)
        => AddRule(r => values.Contains(property(r)));

    /// <inheritdoc/>
    public IRuleBuilder<TRequest> In(string message, IEnumerable<TProperty> values)
        => AddRule(r => values.Contains(property(r)), message);

    /// <inheritdoc/>
    public IRuleBuilder<TRequest> OneOf(IEnumerable<TProperty> values)
        => In(values);

    /// <inheritdoc/>
    public IRuleBuilder<TRequest> OneOf(string message, IEnumerable<TProperty> values)
        => In(message, values);
}

/// <summary>
/// Provides a concrete implementation of <see cref="AbstractShouldBuilder{TRequest, TProperty}"/> for negative validation conditions.
/// This builder inverts the results of validation conditions.
/// </summary>
/// <typeparam name="TRequest">The type of request being validated.</typeparam>
/// <typeparam name="TProperty">The type of the property being validated.</typeparam>
public class ShouldNotBeBuilder<TRequest, TProperty>(
    IRuleBuilder<TRequest> builder, Func<TRequest, TProperty> property, string propertyName) :
    AbstractShouldBuilder<TRequest, TProperty>(builder, property, propertyName)
    where TRequest : class
{
    /// <inheritdoc/>
    protected override bool InvertResults => true;
}

/// <summary>
/// Provides a concrete implementation of <see cref="AbstractShouldBuilder{TRequest, TProperty}"/> for positive validation conditions.
/// </summary>
/// <typeparam name="TRequest">The type of request being validated.</typeparam>
/// <typeparam name="TProperty">The type of the property being validated.</typeparam>
public class ShouldBeBuilder<TRequest, TProperty>(
    IRuleBuilder<TRequest> builder, Func<TRequest, TProperty> property, string propertyName) :
    AbstractShouldBuilder<TRequest, TProperty>(builder, property, propertyName)
    where TRequest : class
{
    /// <inheritdoc/>
    protected override bool InvertResults => false;
}
