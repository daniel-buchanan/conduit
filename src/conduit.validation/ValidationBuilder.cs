using System.Reflection;
using conduit.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace conduit.validation;

/// <summary>
/// Provides a concrete implementation of <see cref="IValidationBuilder"/> for configuring validators in the Conduit system.
/// </summary>
public class ValidationBuilder : IValidationBuilder
{
    private readonly List<ServiceDescriptor> _descriptors = new();
    
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
        _descriptors.Add(new ServiceDescriptor(
            typeof(TRequest), 
            validator, 
            ServiceLifetime.Singleton));
        _descriptors.Add(new ServiceDescriptor(
            typeof(ValidationStage<TRequest, TResponse>), 
            typeof(ValidationStage<TRequest, TResponse>), 
            ServiceLifetime.Transient));
        return this;
    }

    /// <inheritdoc/>
    public IValidationBuilder WithValidatorFor<TRequest, TResponse, TModelValidator>() 
        where TRequest : class, IRequest<TResponse>
        where TResponse : class
        where TModelValidator : IModelValidator<TRequest, TResponse>
    {
        _descriptors.Add(new ServiceDescriptor(
            typeof(IModelValidator<TRequest, TResponse>), 
            typeof(TModelValidator), 
            ServiceLifetime.Transient));
        _descriptors.Add(new ServiceDescriptor(
            typeof(ValidationStage<TRequest, TResponse>), 
            typeof(ValidationStage<TRequest, TResponse>), 
            ServiceLifetime.Transient));
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
    public IValidationBuilder WithValidatorsFromAssembly(Assembly assembly)
    {
        var types = ReflectionHelper.GetTypesFromAssembly(assembly,t => t == typeof(IModelValidator));
        foreach (var t in types)
        {
            var baseType = t.BaseType;
            var request = baseType.GetGenericArguments()[0];
            var response = baseType.GetGenericArguments()[1];
            var interfaceType = typeof(IModelValidator<,>).MakeGenericType(request, response);
            var stageType = typeof(ValidationStage<,>).MakeGenericType(request, response);
            _descriptors.Add(new ServiceDescriptor(interfaceType, t, ServiceLifetime.Transient));
            _descriptors.Add(new ServiceDescriptor(stageType, stageType, ServiceLifetime.Transient));
        }
        
        return this;
    }

    /// <inheritdoc/>
    public IValidationBuilder ThrowIfValidatorNotFound()
    {
        ThrowExceptionIfValidatorNotFound = true;
        return this;
    }
}