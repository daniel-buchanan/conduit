using conduit.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace conduit.validation;

public static class ConfigurationBuilderExtensions
{
    extension(IConduitConfigurationBuilder builder)
    {
        public IConduitConfigurationBuilder AddValidation()
        {
            var x = builder as ConduitConfigurationBuilder;
            
            return builder.AddValidation(b =>
            {
                var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (var loadedAssembly in loadedAssemblies)
                {
                    b.WithValidatorsFromAssembly(loadedAssembly);
                }
            });
        }

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