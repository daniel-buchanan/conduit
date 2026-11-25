using conduit.Pipes.Stages;
using conduit.validation.Rules;

namespace conduit.validation;

/// <summary>
/// Builds validation conditions for a property, for both <c>Be()</c> and <c>NotBe()</c>.
/// </summary>
/// <typeparam name="TRequest">The type of request being validated.</typeparam>
/// <typeparam name="TProperty">The type of the property being validated.</typeparam>
/// <param name="builder">The underlying rule builder to add rules to.</param>
/// <param name="property">The function that selects the property being validated.</param>
/// <param name="propertyName">The property name reported in validation errors.</param>
/// <param name="invertResults"><c>true</c> for <c>NotBe()</c>: each condition passes when it does not hold.</param>
public class ShouldBeBuilder<TRequest, TProperty>(
    IRuleBuilder<TRequest> builder,
    Func<TRequest, TProperty> property,
    string propertyName,
    bool invertResults) :
    IShouldBeBuilder<TRequest, TProperty>
    where TRequest : class
{
    /// <summary>
    /// Gets the function that selects the property being validated.
    /// </summary>
    protected Func<TRequest, TProperty> Property => property;

    /// <summary>
    /// Adds a rule to the underlying builder, inverting the validator's outcome for <c>NotBe()</c>.
    /// </summary>
    /// <param name="validator">A function that returns whether the property is valid.</param>
    /// <param name="message">An optional custom error message.</param>
    protected IRuleBuilder<TRequest> AddRule(Func<TRequest, bool> validator, string? message = null)
    {
        ValidationResult<TRequest> Execute(TRequest r)
        {
            var result = validator(r);
            if (invertResults) result = !result;
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
    /// <exception cref="ArgumentNullException"><paramref name="values"/> is null. Thrown when the rule executes, not here.</exception>
    public IRuleBuilder<TRequest> In(IEnumerable<TProperty> values)
        => AddRule(r => CheckedIn(values, property(r)));

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException"><paramref name="values"/> is null. Thrown when the rule executes, not here.</exception>
    public IRuleBuilder<TRequest> In(string message, IEnumerable<TProperty> values)
        => AddRule(r => CheckedIn(values, property(r)), message);

    /// <summary>
    /// Checks membership of <paramref name="value"/> in <paramref name="values"/>, guarding <paramref name="values"/>
    /// with a clear, rule-specific <see cref="ArgumentNullException"/> rather than letting <see cref="Enumerable.Contains{TSource}"/>
    /// throw its own, naming a "source" parameter the caller never referred to.
    /// </summary>
    private static bool CheckedIn(IEnumerable<TProperty> values, TProperty value)
    {
        if (values is null) throw new ArgumentNullException(nameof(values));
        return values.Contains(value);
    }

    /// <inheritdoc/>
    public IRuleBuilder<TRequest> OneOf(IEnumerable<TProperty> values)
        => In(values);

    /// <inheritdoc/>
    public IRuleBuilder<TRequest> OneOf(string message, IEnumerable<TProperty> values)
        => In(message, values);
}
