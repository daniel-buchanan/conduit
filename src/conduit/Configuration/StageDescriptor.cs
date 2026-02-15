using conduit.Pipes;
using Microsoft.Extensions.DependencyInjection;

namespace conduit.Configuration;

/// <summary>
/// Represents the configuration for a pipeline stage, including its types and service lifetime.
/// </summary>
/// <param name="request">The request type handled by the stage.</param>
/// <param name="response">The response type produced by the stage.</param>
/// <param name="implementationType">The concrete implementation type of the stage.</param>
/// <param name="interfaceType">The interface type of the stage (optional).</param>
/// <param name="lifetime">The service lifetime for the stage (default: Transient).</param>
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
    /// Gets the concrete implementation type of the stage.
    /// </summary>
    public Type ImplementationType { get; } = implementationType;
    
    /// <summary>
    /// Gets the interface type for this stage.
    /// </summary>
    public Type InterfaceType { get; } = GetInterfaceType(request, response, interfaceType);
    
    /// <summary>
    /// Gets the service lifetime for this stage.
    /// </summary>
    public ServiceLifetime Lifetime { get; set; } = lifetime;

    /// <summary>
    /// Gets the service descriptor for this stage.
    /// </summary>
    public ServiceDescriptor Descriptor => GetServiceDescriptor();

    private static Type GetInterfaceType(Type request, Type response, Type? interfaceType)
    {
        if (interfaceType is not null) return interfaceType;
        var typeArguments = new[] { request, response };
        var genericType = typeof(IPipeStage<,>).MakeGenericType(typeArguments);
        return genericType;
    }
    
    private ServiceDescriptor GetServiceDescriptor() => new(InterfaceType, ImplementationType, Lifetime);
}

/// <summary>
/// Provides a strongly-typed version of <see cref="StageDescriptor"/> for specific request and response types.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
/// <typeparam name="TStage">The stage implementation type.</typeparam>
public class StageDescriptor<TRequest, TResponse, TStage>(Type? interfaceType = null, ServiceLifetime lifetime = ServiceLifetime.Transient) : 
    StageDescriptor(typeof(TRequest), typeof(TResponse), typeof(TStage), interfaceType, lifetime)
    where TRequest : class, IRequest<TResponse>
    where TResponse : class
    where TStage : IPipeStage<TRequest, TResponse>;