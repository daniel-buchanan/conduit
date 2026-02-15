using conduit.validation.Rules;

namespace conduit.validation;

/// <summary>
/// Defines the contract for building validation rules for a request type.
/// </summary>
/// <typeparam name="TRequest">The type of request being validated.</typeparam>
public interface IRuleBuilder<TRequest> where TRequest : class
{
    /// <summary>
    /// Starts defining a validation rule for the specified property.
    /// </summary>
    /// <typeparam name="TProperty">The type of the property being validated.</typeparam>
    /// <param name="property">A function that selects the property to validate.</param>
    /// <returns>A builder for defining validation conditions for the property.</returns>
    IShouldBuilder<TRequest, TProperty> Should<TProperty>(Func<TRequest, TProperty> property);
    
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
    public IShouldBuilder<TRequest, TProperty> Should<TProperty>(Func<TRequest, TProperty> property)
        => new ShouldBuilder<TRequest, TProperty>(this, property);

    /// <inheritdoc/>
    public Rule<TRequest>[] Build()
        => _rules.ToArray();

    /// <inheritdoc/>
    public void AddRule(Rule<TRequest> rule)
        => _rules.Add(rule);
}