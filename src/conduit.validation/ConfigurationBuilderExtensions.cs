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

        foreach (var descriptor in descriptors)
        {
            builder.AddDescriptor(descriptor);
        }

        // Registered once, as an open generic: DI can construct ValidationStage<TRequest, TResponse> for
        // ANY request/response pair on demand, regardless of whether a specific IModelValidator<,> was
        // discovered for that pair. Without this, ValidationStage<,> would only resolve for pairs that
        // happen to have a validator, and every other request type's default validation stage would fail
        // to resolve at all (StageNotFoundException) instead of reaching the "no validator found" handling
        // inside ValidationStage itself.
        builder.AddDescriptor(new ServiceDescriptor(typeof(ValidationStage<,>), typeof(ValidationStage<,>), ServiceLifetime.Transient));

        var configInstance = new ConduitValidationConfiguration(validationBuilder.ThrowExceptionIfValidatorNotFound);
        builder.AddDescriptor(new ServiceDescriptor(typeof(ConduitValidationConfiguration), configInstance));

        builder.AddDefaultPreExecutionStage(typeof(ValidationStage<,>));

        return builder;
    }
}