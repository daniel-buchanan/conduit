using System.Reflection;
using System.Runtime.CompilerServices;
using conduit.Helpers;
using Microsoft.Extensions.DependencyInjection;

[assembly: InternalsVisibleTo("conduit.tests")]

namespace conduit.validation;

/// <summary>
/// Provides a concrete implementation of <see cref="IValidationBuilder"/> for configuring validators in the Conduit system.
/// </summary>
public class ValidationBuilder : IValidationBuilder
{
    private readonly List<ServiceDescriptor> _descriptors = new();
    private readonly HashSet<Type> _scannedValidatorInterfaces = new();
    private readonly HashSet<Type> _explicitlyRegisteredValidatorInterfaces = new();
    
    /// <summary>
    /// Gets a value indicating whether to throw an exception if a validator is not found.
    /// </summary>
    public bool ThrowExceptionIfValidatorNotFound { get; private set; }
    
    /// <summary>
    /// Builds and returns the configured service descriptors.
    /// </summary>
    /// <returns>An enumerable collection of service descriptors.</returns>
    public IEnumerable<ServiceDescriptor> Build() => _descriptors;
    
    /// <inheritdoc/>
    public IValidationBuilder WithValidatorFor<TRequest, TResponse>(IModelValidator<TRequest, TResponse> validator)
        where TRequest : class, IRequest<TResponse>
        where TResponse : class
    {
        _explicitlyRegisteredValidatorInterfaces.Add(typeof(IModelValidator<TRequest, TResponse>));
        _descriptors.Add(new ServiceDescriptor(
            typeof(IModelValidator<TRequest, TResponse>),
            validator));
        return this;
    }

    /// <inheritdoc/>
    public IValidationBuilder WithValidatorFor<TRequest, TResponse, TModelValidator>()
        where TRequest : class, IRequest<TResponse>
        where TResponse : class
        where TModelValidator : IModelValidator<TRequest, TResponse>
    {
        _explicitlyRegisteredValidatorInterfaces.Add(typeof(IModelValidator<TRequest, TResponse>));
        _descriptors.Add(new ServiceDescriptor(
            typeof(IModelValidator<TRequest, TResponse>),
            typeof(TModelValidator),
            ServiceLifetime.Singleton));
        return this;
    }

    /// <inheritdoc/>
    public IValidationBuilder WithValidatorsFromAssembly<TLocator>()
    {
        var locatorType = typeof(TLocator);
        var assembly = locatorType.Assembly;
        return WithValidatorsFromAssembly(assembly);
    }

    /// <inheritdoc/>
    /// <exception cref="ValidatorAlreadyRegisteredException">
    /// Two assembly-scan discoveries (in this call or a prior one) collide on the same (TRequest, TResponse) pair.
    /// See ADR-0003.
    /// </exception>
    public IValidationBuilder WithValidatorsFromAssembly(Assembly assembly)
    {
        var types = ReflectionHelper.GetTypesFromAssembly(assembly,t => t == typeof(IModelValidator));
        foreach (var t in types)
        {
            var (request, response) = GetRequestResponseTypes(t);
            var interfaceType = typeof(IModelValidator<,>).MakeGenericType(request, response);

            // An explicit registration always wins over a scanned one, regardless of which call happened
            // first (ADR-0003) — so a scan discovering a pair that's already explicitly registered is
            // superseded, not a collision, and is skipped silently rather than added or thrown on.
            if (_explicitlyRegisteredValidatorInterfaces.Contains(interfaceType)) continue;

            if (!_scannedValidatorInterfaces.Add(interfaceType))
                throw new ValidatorAlreadyRegisteredException(
                    $"A validator for '{request.Name}' -> '{response.Name}' was already discovered by an assembly scan. " +
                    $"Use {nameof(WithValidatorFor)} to explicitly override a scanned validator.");

            _descriptors.Add(new ServiceDescriptor(interfaceType, t, ServiceLifetime.Singleton));
        }

        return this;
    }

    /// <inheritdoc/>
    public IValidationBuilder ThrowIfValidatorNotFound()
    {
        ThrowExceptionIfValidatorNotFound = true;
        return this;
    }

    /// <summary>
    /// Extracts the TRequest/TResponse type arguments from a discovered validator's <see cref="ModelValidator{TRequest, TResponse}"/>
    /// base type. Internal (rather than private) so its guard behavior can be unit tested directly without needing
    /// an assembly that contains an intentionally-invalid validator type.
    /// </summary>
    internal static (Type Request, Type Response) GetRequestResponseTypes(Type validatorType)
    {
        var baseType = validatorType.BaseType;
        if (baseType is null || baseType.GetGenericArguments().Length < 2)
            throw new InvalidOperationException(
                $"'{validatorType.FullName}' implements {nameof(IModelValidator)} directly. " +
                $"Validators discovered via {nameof(WithValidatorsFromAssembly)} must derive from ModelValidator<TRequest, TResponse>.");

        return (baseType.GetGenericArguments()[0], baseType.GetGenericArguments()[1]);
    }
}