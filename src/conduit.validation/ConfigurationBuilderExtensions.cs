using System.Reflection;
using conduit.common.Helpers;
using conduit.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace conduit.validation;

public static class ConfigurationBuilderExtensions
{
    extension(IConduitConfigurationBuilder builder)
    {
        public IConduitConfigurationBuilder AddValidation()
            => builder.AddValidation(b =>
            {
                var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (var loadedAssembly in loadedAssemblies)
                {
                    b.WithValidatorsFromAssembly(loadedAssembly);
                }
            });

        public IConduitConfigurationBuilder AddValidation(Action<IValidationBuilder> options)
        {
            var validationBuilder = new ValidationBuilder();
            options(validationBuilder);

            var descriptors = validationBuilder.Build();
            var conduitBuilder = builder as ConduitConfigurationBuilder;
        
            foreach (var descriptor in descriptors)
            {
                conduitBuilder!.AddDescriptor(descriptor);
            }

            var configInstance = new ConduitValidationConfiguration(validationBuilder.ThrowExceptionIfValidatorNotFound);
            conduitBuilder!.AddDescriptor(new ServiceDescriptor(typeof(ConduitValidationConfiguration), configInstance));
            return builder;
        }
    }
}

public interface IValidationBuilder
{
    IValidationBuilder WithValidatorFor<TRequest>(IModelValidator<TRequest> validator);
    IValidationBuilder WithValidatorFor<TRequest, TModelValidator>()
        where TModelValidator : IModelValidator<TRequest>;

    IValidationBuilder WithValidatorsFromAssembly<TLocator>();
    IValidationBuilder WithValidatorsFromAssembly(Assembly assembly);
    IValidationBuilder ThrowIfValidatorNotFound();
}

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