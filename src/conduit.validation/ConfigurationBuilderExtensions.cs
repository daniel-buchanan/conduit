using conduit.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace conduit.validation;

/// <summary>
/// Provides extension methods for <see cref="IConduitConfigurationBuilder"/> to add validation support.
/// </summary>
public static class ConfigurationBuilderExtensions
{
    /// <summary>
    /// Adds validation support to the Conduit configuration by automatically discovering and registering validators from all loaded assemblies.
    /// </summary>
    /// <param name="builder">The Conduit configuration builder.</param>
    /// <returns>The configuration builder for method chaining.</returns>
    public static IConduitConfigurationBuilder AddValidation(this IConduitConfigurationBuilder builder)
    {
        return builder.AddValidation(b =>
        {
            var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var loadedAssembly in loadedAssemblies)
            {
                b.WithValidatorsFromAssembly(loadedAssembly);
            }
        });
    }

    /// <summary>
    /// Adds validation support to the Conduit configuration with custom validation builder options.
    /// </summary>
    /// <param name="builder">The Conduit configuration builder.</param>
    /// <param name="options">An action to configure the validation builder.</param>
    /// <returns>The configuration builder for method chaining.</returns>
    public static IConduitConfigurationBuilder AddValidation(this IConduitConfigurationBuilder builder, Action<IValidationBuilder> options)
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
        
        conduitBuilder.AddDefaultPreExecutionStage(typeof(ValidationStage<,>));
        
        return builder;
    }
}