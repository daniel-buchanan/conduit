using System.Linq.Expressions;

namespace conduit.validation;

/// <summary>
/// Defines the contract for selecting a condition (Be or NotBe) for the current rule.
/// </summary>
/// <typeparam name="TRequest">The type of request being validated.</typeparam>
/// <typeparam name="TProperty">The type of the property being validated.</typeparam>
public interface IShouldBuilder<TRequest, in TProperty> where TRequest : class
{
    /// <summary>
    /// Specifies that the property should match the following validation conditions.
    /// </summary>
    /// <returns>A builder for defining positive validation conditions.</returns>
    IShouldBeBuilder<TRequest, TProperty> Be();

    /// <summary>
    /// Specifies that the property should NOT match the following validation conditions.
    /// </summary>
    /// <returns>A builder for defining negative validation conditions.</returns>
    IShouldBeBuilder<TRequest, TProperty> NotBe();
}

/// <summary>
/// Provides a concrete implementation of <see cref="IShouldBuilder{TRequest, TProperty}"/> for building validation conditions.
/// Extracts the property name from the expression up front, at construction time, so an invalid expression shape
/// fails as soon as <c>Should()</c> is called rather than when a terminal rule method is invoked.
/// </summary>
/// <typeparam name="TRequest">The type of request being validated.</typeparam>
/// <typeparam name="TProperty">The type of the property being validated.</typeparam>
public class ShouldBuilder<TRequest, TProperty> : IShouldBuilder<TRequest, TProperty> where TRequest : class
{
    private readonly IRuleBuilder<TRequest> _builder;
    private readonly Func<TRequest, TProperty> _property;
    private readonly string _propertyName;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShouldBuilder{TRequest, TProperty}"/> class.
    /// </summary>
    /// <param name="builder">The underlying rule builder to add rules to.</param>
    /// <param name="property">A member-access expression selecting the property being validated.</param>
    /// <exception cref="ArgumentException">The expression is not a pure member-access chain.</exception>
    public ShouldBuilder(IRuleBuilder<TRequest> builder, Expression<Func<TRequest, TProperty>> property)
    {
        _builder = builder;
        (_property, _propertyName) = PropertyNameExtractor.ExtractAndCompile(property);
    }

    /// <inheritdoc/>
    public IShouldBeBuilder<TRequest, TProperty> Be()
        => new ShouldBeBuilder<TRequest, TProperty>(_builder, _property, _propertyName);

    /// <inheritdoc/>
    public IShouldBeBuilder<TRequest, TProperty> NotBe()
        => new ShouldNotBeBuilder<TRequest, TProperty>(_builder, _property, _propertyName);
}
