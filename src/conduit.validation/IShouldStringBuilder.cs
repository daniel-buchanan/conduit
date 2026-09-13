namespace conduit.validation;

/// <summary>
/// Defines the contract for selecting a validation rule condition (Be or NotBe) for a string property.
/// </summary>
/// <typeparam name="TRequest">The type of request being validated.</typeparam>
public interface IShouldStringBuilder<TRequest> where TRequest : class
{
    /// <summary>
    /// Specifies that the property should match the following validation conditions.
    /// </summary>
    /// <returns>A builder for defining positive validation conditions.</returns>
    IShouldBeStringBuilder<TRequest> Be();

    /// <summary>
    /// Specifies that the property should NOT match the following validation conditions.
    /// </summary>
    /// <returns>A builder for defining negative validation conditions.</returns>
    IShouldBeStringBuilder<TRequest> NotBe();
}

/// <summary>
/// Defines the contract for building validation rules for a string property, adding string-specific conditions
/// on top of <see cref="IShouldBeBuilder{TRequest, TProperty}"/>.
/// </summary>
/// <typeparam name="TRequest">The type of request being validated.</typeparam>
public interface IShouldBeStringBuilder<TRequest> : IShouldBeBuilder<TRequest, string> where TRequest : class
{
    /// <summary>
    /// Validates that the property is null or consists of whitespace only.
    /// </summary>
    /// <param name="message">An optional custom error message.</param>
    /// <returns>The rule builder for method chaining.</returns>
    IRuleBuilder<TRequest> NullOrWhitespace(string? message = null);
}
