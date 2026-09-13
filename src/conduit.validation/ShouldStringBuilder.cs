namespace conduit.validation;

/// <summary>
/// Provides a concrete implementation of <see cref="IShouldStringBuilder{TRequest}"/> for building validation
/// conditions for a string property.
/// </summary>
/// <typeparam name="TRequest">The type of request being validated.</typeparam>
public class ShouldStringBuilder<TRequest>(
    IRuleBuilder<TRequest> builder,
    Func<TRequest, string> property) :
    IShouldStringBuilder<TRequest> where TRequest : class
{
    /// <inheritdoc/>
    public IShouldBeStringBuilder<TRequest> Be()
        => new ShouldBeStringBuilder<TRequest>(builder, property, invertResults: false);

    /// <inheritdoc/>
    public IShouldBeStringBuilder<TRequest> NotBe()
        => new ShouldBeStringBuilder<TRequest>(builder, property, invertResults: true);
}

/// <summary>
/// Provides a concrete implementation of <see cref="IShouldBeStringBuilder{TRequest}"/>, adding string-specific
/// validation conditions on top of the shared <see cref="AbstractShouldBuilder{TRequest, TProperty}"/> behavior.
/// </summary>
/// <typeparam name="TRequest">The type of request being validated.</typeparam>
public class ShouldBeStringBuilder<TRequest>(
    IRuleBuilder<TRequest> builder,
    Func<TRequest, string> property,
    bool invertResults) :
    AbstractShouldBuilder<TRequest, string>(builder, property), IShouldBeStringBuilder<TRequest>
    where TRequest : class
{
    /// <inheritdoc/>
    protected override bool InvertResults => invertResults;

    /// <inheritdoc/>
    public IRuleBuilder<TRequest> NullOrWhitespace(string? message = null)
        => AddRule(r => string.IsNullOrWhiteSpace(Property(r)), message);
}
