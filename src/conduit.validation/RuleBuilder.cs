using System.Linq.Expressions;
using conduit.validation.Rules;

namespace conduit.validation;

/// <summary>
/// Defines the contract for building validation rules for a request type.
/// </summary>
/// <typeparam name="TRequest">The type of request being validated.</typeparam>
public interface IRuleBuilder<TRequest> where TRequest : class
{
    /// <summary>
    /// Starts defining a validation rule for the specified property. The expression must be a simple member-access
    /// chain (e.g. <c>x =&gt; x.Foo</c> or <c>x =&gt; x.Foo.Bar</c>) so the real property name can be extracted for
    /// <see cref="ValidationError.PropertyName"/>.
    /// </summary>
    /// <typeparam name="TProperty">The type of the property being validated.</typeparam>
    /// <param name="property">An expression that selects the property to validate.</param>
    /// <returns>A builder for defining validation conditions for the property.</returns>
    /// <exception cref="ArgumentException">The expression is not a pure member-access chain.</exception>
    IShouldBuilder<TRequest, TProperty> Should<TProperty>(Expression<Func<TRequest, TProperty>> property);

    /// <summary>
    /// Starts defining a validation rule for the specified string property, exposing string-specific conditions
    /// (such as <see cref="IShouldBeStringBuilder{TRequest}.NullOrWhitespace"/>) that don't make sense for other types.
    /// The expression must be a simple member-access chain (see <see cref="Should{TProperty}"/>).
    /// </summary>
    /// <param name="property">An expression that selects the string property to validate.</param>
    /// <returns>A builder for defining validation conditions for the property.</returns>
    /// <exception cref="ArgumentException">The expression is not a pure member-access chain.</exception>
    IShouldStringBuilder<TRequest> Should(Expression<Func<TRequest, string?>> property);

    /// <summary>
    /// When implemented, builds and returns the configured rules.
    /// </summary>
    /// <returns>An array of configured validation rules.</returns>
    internal Rule<TRequest>[] Build();
    
    /// <summary>
    /// When implemented, adds a rule to the builder.
    /// </summary>
    /// <param name="rule">The rule to add.</param>
    internal void AddRule(Rule<TRequest> rule);
}

/// <summary>
/// Provides a concrete implementation of <see cref="IRuleBuilder{TRequest}"/> for building validation rules.
/// </summary>
/// <typeparam name="TRequest">The type of request being validated.</typeparam>
public class RuleBuilder<TRequest> : IRuleBuilder<TRequest> where TRequest : class
{
    private readonly List<Rule<TRequest>> _rules = new();
    
    /// <inheritdoc/>
    public IShouldBuilder<TRequest, TProperty> Should<TProperty>(Expression<Func<TRequest, TProperty>> property)
        => new ShouldBuilder<TRequest, TProperty>(this, property);

    /// <inheritdoc/>
    public IShouldStringBuilder<TRequest> Should(Expression<Func<TRequest, string?>> property)
        => new ShouldStringBuilder<TRequest>(this, property);

    /// <inheritdoc/>
    public Rule<TRequest>[] Build()
        => _rules.ToArray();

    /// <inheritdoc/>
    public void AddRule(Rule<TRequest> rule)
        => _rules.Add(rule);
}