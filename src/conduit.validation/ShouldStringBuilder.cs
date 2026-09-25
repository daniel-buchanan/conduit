using System.Linq.Expressions;

namespace conduit.validation;

/// <summary>
/// Provides a concrete implementation of <see cref="IShouldStringBuilder{TRequest}"/> for building validation
/// conditions for a string property. Extracts the property name from the expression up front, at construction
/// time, so an invalid expression shape fails as soon as <c>Should()</c> is called.
/// </summary>
/// <typeparam name="TRequest">The type of request being validated.</typeparam>
public class ShouldStringBuilder<TRequest> : IShouldStringBuilder<TRequest> where TRequest : class
{
    private readonly IRuleBuilder<TRequest> _builder;
    private readonly Func<TRequest, string?> _property;
    private readonly string _propertyName;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShouldStringBuilder{TRequest}"/> class.
    /// </summary>
    /// <param name="builder">The underlying rule builder to add rules to.</param>
    /// <param name="property">A member-access expression selecting the string property being validated.</param>
    /// <exception cref="ArgumentException">The expression is not a pure member-access chain.</exception>
    public ShouldStringBuilder(IRuleBuilder<TRequest> builder, Expression<Func<TRequest, string?>> property)
    {
        _builder = builder;
        (_property, _propertyName) = PropertyNameExtractor.ExtractAndCompile(property);
    }

    /// <inheritdoc/>
    public IShouldBeStringBuilder<TRequest> Be()
        => new ShouldBeStringBuilder<TRequest>(_builder, _property, _propertyName, invertResults: false);

    /// <inheritdoc/>
    public IShouldBeStringBuilder<TRequest> NotBe()
        => new ShouldBeStringBuilder<TRequest>(_builder, _property, _propertyName, invertResults: true);
}

/// <summary>
/// Provides a concrete implementation of <see cref="IShouldBeStringBuilder{TRequest}"/>, adding string-specific
/// validation conditions on top of the shared <see cref="AbstractShouldBuilder{TRequest, TProperty}"/> behavior.
/// </summary>
/// <typeparam name="TRequest">The type of request being validated.</typeparam>
public class ShouldBeStringBuilder<TRequest>(
    IRuleBuilder<TRequest> builder,
    Func<TRequest, string?> property,
    string propertyName,
    bool invertResults) :
    AbstractShouldBuilder<TRequest, string?>(builder, property, propertyName), IShouldBeStringBuilder<TRequest>
    where TRequest : class
{
    /// <inheritdoc/>
    protected override bool InvertResults => invertResults;

    /// <inheritdoc/>
    public IRuleBuilder<TRequest> NullOrWhitespace(string? message = null)
        => AddRule(r => string.IsNullOrWhiteSpace(Property(r)), message);
}
