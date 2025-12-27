using conduit.Pipes;
using Microsoft.Extensions.DependencyInjection;

namespace conduit.Configuration;

public class StageDescriptor(Type request, Type response, Type implementationType, Type? interfaceType = null, ServiceLifetime lifetime = ServiceLifetime.Transient)
{
    /// <summary>
    /// Gets the type of the request.
    /// </summary>
    public Type RequestType { get; } = request;

    /// <summary>
    /// Gets the type of the response.
    /// </summary>
    public Type ResponseType { get; } = response;

    /// <summary>
    /// Gets the type of the stage.
    /// </summary>
    public Type ImplementationType { get; } = implementationType;
    
    /// <summary>
    /// Gets the interface type for this stage.
    /// </summary>
    public Type InterfaceType { get; } = GetInterfaceType(request, response, interfaceType);
    
    /// <summary>
    /// The lifetime of the Pipeline Stage
    /// </summary>
    public ServiceLifetime Lifetime { get; set; } = lifetime;

    /// <summary>
    /// The service descriptor for this stage.
    /// </summary>
    public ServiceDescriptor Descriptor => GetServiceDescriptor();

    private static Type GetInterfaceType(Type request, Type response, Type? interfaceType)
    {
        if (interfaceType is not null) return interfaceType;
        var typeArguments = new Type[] { request, response };
        var genericType = typeof(IPipeStage<,>).MakeGenericType(typeArguments);
        return genericType;
    }
    
    private ServiceDescriptor GetServiceDescriptor() => new(InterfaceType, ImplementationType, Lifetime);
}

public class StageDescriptor<TRequest, TResponse, TStage>(Type? interfaceType = null, ServiceLifetime lifetime = ServiceLifetime.Transient) : 
    StageDescriptor(typeof(TRequest), typeof(TResponse), typeof(TStage), interfaceType, lifetime)
    where TRequest : class, IRequest<TResponse>
    where TResponse : class
    where TStage : IPipeStage<TRequest, TResponse>;