namespace conduit.validation;

/// <summary>
/// Defines the contract for selecting a validation rule condition (Be or ShouldNot).
/// </summary>
/// <typeparam name="TRequest">The type of request being validated.</typeparam>
/// <typeparam name="in TProperty">The type of the property being validated.</typeparam>
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
/// </summary>
/// <typeparam name="TRequest">The type of request being validated.</typeparam>
/// <typeparam name="TProperty">The type of the property being validated.</typeparam>
public class ShouldBuilder<TRequest, TProperty>(
    IRuleBuilder<TRequest> builder,
    Func<TRequest, TProperty> property) : 
    IShouldBuilder<TRequest, TProperty> where TRequest : class
{
    /// <inheritdoc/>
    public IShouldBeBuilder<TRequest, TProperty> Be()
        => new ShouldBeBuilder<TRequest, TProperty>(builder, property);

    /// <inheritdoc/>
    public IShouldBeBuilder<TRequest, TProperty> NotBe()
        => new ShouldNotBeBuilder<TRequest, TProperty>(builder, property);
}