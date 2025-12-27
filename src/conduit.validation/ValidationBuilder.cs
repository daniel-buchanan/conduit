using System.Reflection;
using conduit.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace conduit.validation;

public class ValidationBuilder : IValidationBuilder
{
    private readonly List<ServiceDescriptor> _descriptors = new();
    
    public bool ThrowExceptionIfValidatorNotFound { get; private set; }
    
    public IEnumerable<ServiceDescriptor> Build() => _descriptors;
    
    public IValidationBuilder WithValidatorFor<TRequest>(IModelValidator<TRequest> validator)
    {
        _descriptors.Add(new ServiceDescriptor(typeof(TRequest), validator, ServiceLifetime.Singleton));
        return this;
    }

    public IValidationBuilder WithValidatorFor<TRequest, TModelValidator>() where TModelValidator : IModelValidator<TRequest>
    {
        _descriptors.Add(new ServiceDescriptor(typeof(TRequest), typeof(TModelValidator), ServiceLifetime.Transient));
        return this;
    }

    public IValidationBuilder WithValidatorsFromAssembly<TLocator>()
    {
        var locatorType = typeof(TLocator);
        var assembly = Assembly.GetAssembly(locatorType);
        return assembly == null 
            ? this 
            : WithValidatorsFromAssembly(assembly);
    }

    public IValidationBuilder WithValidatorsFromAssembly(Assembly assembly)
    {
        var types = ReflectionHelper.GetTypesFromAssembly(assembly,t => t.IsSubclassOf(typeof(IModelValidator)));
        foreach (var t in types)
        {
            var genericParameter = t.GetGenericArguments()[0];
            var interfaceType = typeof(IModelValidator<>).MakeGenericType(genericParameter);
            _descriptors.Add(new ServiceDescriptor(interfaceType, t, ServiceLifetime.Transient));
        }
        
        return this;
    }

    public IValidationBuilder ThrowIfValidatorNotFound()
    {
        ThrowExceptionIfValidatorNotFound = true;
        return this;
    }
}