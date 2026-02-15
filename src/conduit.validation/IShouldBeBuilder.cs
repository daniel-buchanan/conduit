namespace conduit.validation;

/// <summary>
/// Defines the contract for building validation rules for properties in a request.
/// </summary>
/// <typeparam name="TRequest">The type of request being validated.</typeparam>
/// <typeparam name="in TProperty">The type of the property being validated.</typeparam>
public interface IShouldBeBuilder<TRequest, in TProperty> where TRequest : class
{
    /// <summary>
    /// Validates that the property is null.
    /// </summary>
    /// <param name="message">An optional custom error message.</param>
    /// <returns>The rule builder for method chaining.</returns>
    IRuleBuilder<TRequest> Null(string? message = null);
    
    /// <summary>
    /// Validates that the property is null or consists of whitespace only.
    /// </summary>
    /// <param name="message">An optional custom error message.</param>
    /// <returns>The rule builder for method chaining.</returns>
    IRuleBuilder<TRequest> NullOrWhitespace(string? message = null);
    
    /// <summary>
    /// Validates that the property value equals the specified value.
    /// </summary>
    /// <param name="value">The value to compare against.</param>
    /// <param name="message">An optional custom error message.</param>
    /// <returns>The rule builder for method chaining.</returns>
    IRuleBuilder<TRequest> EqualTo(TProperty value, string? message = null);
    
    /// <summary>
    /// Validates that the property value is contained within the specified collection.
    /// </summary>
    /// <param name="values">The collection of allowed values.</param>
    /// <returns>The rule builder for method chaining.</returns>
    IRuleBuilder<TRequest> In(IEnumerable<TProperty> values);
    
    /// <summary>
    /// Validates that the property value is contained within the specified collection with a custom error message.
    /// </summary>
    /// <param name="message">The custom error message.</param>
    /// <param name="values">The collection of allowed values.</param>
    /// <returns>The rule builder for method chaining.</returns>
    IRuleBuilder<TRequest> In(string message, IEnumerable<TProperty> values);
    
    /// <summary>
    /// Validates that the property value is one of the specified values (alias for In).
    /// </summary>
    /// <param name="values">The collection of allowed values.</param>
    /// <returns>The rule builder for method chaining.</returns>
    IRuleBuilder<TRequest> OneOf(IEnumerable<TProperty> values);
    
    /// <summary>
    /// Validates that the property value is one of the specified values with a custom error message (alias for In).
    /// </summary>
    /// <param name="message">The custom error message.</param>
    /// <param name="values">The collection of allowed values.</param>
    /// <returns>The rule builder for method chaining.</returns>
    IRuleBuilder<TRequest> OneOf(string message, IEnumerable<TProperty> values);
}