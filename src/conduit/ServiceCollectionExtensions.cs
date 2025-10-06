using conduit.common;
using conduit.Configuration;
using conduit.logging;
using Microsoft.Extensions.DependencyInjection;

namespace conduit;

/// <summary>
/// Provides extension methods for <see cref="IServiceCollection"/> to register Conduit services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the core Conduit services to the specified <see cref="IServiceCollection"/>.
    /// This overload registers default implementations for <see cref="IConduit"/>, <see cref="ILog"/>, and <see cref="IEnvironment"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="logger">A logger instance to use, if not provided the default console logger will be used.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddConduit(this IServiceCollection services, ILog? logger = null)
    {
        services.AddSingleton<IConduit, Conduit>();
        if(logger != null) services.AddSingleton<ILog>(logger);
        else services.AddScoped<ILog, ConsoleLog>();
        services.AddSingleton<IEnvironment, EnvironmentImpl>();
        return services;
    }

    /// <summary>
    /// Adds the core Conduit services to the specified <see cref="IServiceCollection"/>
    /// and allows for custom configuration of the Conduit system.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="configure">An action to configure the <see cref="IConduitConfigurationBuilder"/>.</param>
    /// <param name="logger">A logger instance to use, if not provided the default console logger will be used.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddConduit(
        this IServiceCollection services,
        Action<IConduitConfigurationBuilder> configure,
        ILog? logger = null)
    {
        services.AddConduit(logger);
        var configuration = new ConduitConfiguration(HashUtil.Instance);
        var builder = new CondiutConfigurationBuilder(configuration);
        configure(builder);
        builder.Build(services);
        return services;
    }

    /// <summary>
    /// Adds a range of service descriptors to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the descriptors to.</param>
    /// <param name="defs">An array of <see cref="ServiceDescriptor"/> to add.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    internal static IServiceCollection AddRange(this IServiceCollection services, params ServiceDescriptor[] defs)
    {
        foreach(var def in defs)
            services.Add(def);
        return services;
    }
}