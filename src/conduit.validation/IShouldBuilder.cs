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
