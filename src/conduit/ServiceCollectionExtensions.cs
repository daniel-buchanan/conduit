using conduit.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace conduit;

public static class ServiceCollectionExtensions
{
    public static IConduitConfigurationBuilder AddConduit(this IServiceCollection services)
    {
        services.AddSingleton<IConduit, Conduit>();
        return new CondiutConfigurationBuilder();
    }

    public static IConduitConfigurationBuilder AddConduit(this IServiceCollection services, params Type[] locatorTypes)
    {
        return AddConduit(services);
    }
}