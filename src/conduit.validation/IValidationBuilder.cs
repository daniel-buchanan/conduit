using System.Reflection;

namespace conduit.validation;

/// <summary>
/// Defines the contract for building validation configuration for the Conduit system.
/// </summary>
public interface IValidationBuilder
{
    /// <summary>
    /// Registers a specific validator instance for a request and response type pair.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="validator">The validator instance to register.</param>
    /// <returns>The current validation builder instance for method chaining.</returns>
    IValidationBuilder WithValidatorFor<TRequest, TResponse>(IModelValidator<TRequest, TResponse> validator)
        where TRequest : class, IRequest<TResponse>
        where TResponse : class;
    
    /// <summary>
    /// Registers a validator type for a request and response type pair.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <typeparam name="TModelValidator">The validator type to register.</typeparam>
    /// <returns>The current validation builder instance for method chaining.</returns>
    IValidationBuilder WithValidatorFor<TRequest, TResponse, TModelValidator>()
        where TRequest : class, IRequest<TResponse>
        where TResponse : class
        where TModelValidator : IModelValidator<TRequest, TResponse>;

    /// <summary>
    /// Registers all validators found in the assembly containing the specified locator type.
    /// </summary>
    /// <typeparam name="TLocator">A type from the assembly to scan for validators.</typeparam>
    /// <returns>The current validation builder instance for method chaining.</returns>
    /// <exception cref="ValidatorAlreadyRegisteredException">
    /// Two assembly-scan discoveries (in this call or a prior one) collide on the same (TRequest, TResponse) pair.
    /// </exception>
    IValidationBuilder WithValidatorsFromAssembly<TLocator>();

    /// <summary>
    /// Registers all validators found in the specified assembly.
    /// </summary>
    /// <param name="assembly">The assembly to scan for validators.</param>
    /// <returns>The current validation builder instance for method chaining.</returns>
    /// <exception cref="ValidatorAlreadyRegisteredException">
    /// Two assembly-scan discoveries (in this call or a prior one) collide on the same (TRequest, TResponse) pair.
    /// </exception>
    IValidationBuilder WithValidatorsFromAssembly(Assembly assembly);
    
    /// <summary>
    /// Configures the validation builder to throw an exception if a validator is not found for a request type.
    /// </summary>
    /// <returns>The current validation builder instance for method chaining.</returns>
    IValidationBuilder ThrowIfValidatorNotFound();
}