using conduit.common;
using conduit.Pipes;
using Microsoft.Extensions.DependencyInjection;

namespace conduit.Configuration;

/// <summary>
/// Represents the configuration for the Conduit system, managing pipe stages and their descriptors.
/// </summary>
/// <param name="hash">The utility for hashing type names.</param>
public class ConduitConfiguration(IHashUtil hash) : IConduitConfiguration
{
    private readonly Dictionary<string, List<ServiceDescriptor>> _pipes = new();
    private readonly Dictionary<string, PipeDescriptor> _pipeDescriptors = new();

    /// <summary>
    /// Gets an enumerable collection of pipe descriptors, each representing a configured pipe.
    /// </summary>
    /// <returns>An enumerable collection of <see cref="PipeDescriptor"/> objects.</returns>
    public IEnumerable<PipeDescriptor> GetPipes()
    {
        foreach (var p in _pipeDescriptors)
        {
            p.Value.SetStages(_pipes[p.Key]);
            yield return p.Value;
        }
    }
    
    /// <summary>
    /// Adds a pipe stage to the Conduit configuration.
    /// This method is a convenience wrapper for the non-generic AddPipeStage.
    /// </summary>
    /// <typeparam name="TStage">The type of the pipe stage to add.</typeparam>
    /// <typeparam name="TRequest">The type of the request handled by the pipe stage.</typeparam>
    /// <typeparam name="TResponse">The type of the response produced by the pipe stage.</typeparam>
    public void AddPipeStage<TStage, TRequest, TResponse>()
        where TStage : IPipeStage<TRequest, TResponse>
        where TRequest : class, IRequest<TResponse>
        where TResponse : class
        => AddPipeStage(typeof(TRequest), typeof(TStage));
    
    /// <summary>
    /// Adds a pipe stage to the Conduit configuration using non-generic types.
    /// This method is used internally to register pipe stages and their corresponding request/response types.
    /// </summary>
    /// <param name="requestType">The type of the request.</param>
    /// <param name="implementationType">The concrete implementation type of the pipe stage.</param>
    /// <param name="interfaceType">The interface type of the pipe stage (optional).</param>
    public void AddPipeStage(Type requestType, Type implementationType, Type? interfaceType = null)
    {
        var typeHash = hash.TypeNameHash(requestType);
        var existing = _pipes.ContainsKey(typeHash);
        if (!existing)
        {
            _pipes.Add(typeHash, []);
            var implementedInterface = requestType.GetInterfaces().FirstOrDefault(i => i.Name.StartsWith("IRequest"));
            var responseType = implementedInterface.GetGenericArguments()[0];
            _pipeDescriptors.Add(typeHash, new PipeDescriptor(requestType, responseType));
        }
        
        var serviceDescriptor = new ServiceDescriptor(implementationType, implementationType, ServiceLifetime.Transient);
        if(interfaceType != null)
            serviceDescriptor = new ServiceDescriptor(interfaceType, implementationType, ServiceLifetime.Transient);
        
        _pipes[typeHash].Add(serviceDescriptor);
    }
}

/// <summary>
/// Represents a descriptor for a Conduit pipe, containing information about the request type, response type, and its stages.
/// </summary>
/// <param name="request">The type of the request.</param>
/// <param name="response">The type of the response.</param>
public class PipeDescriptor(Type request, Type response)
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
    /// Gets or sets the list of service descriptors representing the stages in the pipe.
    /// </summary>
    public List<ServiceDescriptor> Stages { get; private set; } = new();
    
    /// <summary>
    /// Sets the stages for this pipe descriptor.
    /// </summary>
    /// <param name="stages">An enumerable collection of service descriptors representing the stages.</param>
    public void SetStages(IEnumerable<ServiceDescriptor> stages)
        => Stages = stages.ToList();
}

/// <summary>
/// Represents a generic descriptor for a Conduit pipe, with strongly typed request and response types.
/// </summary>
/// <typeparam name="TRequest">The type of the request.</typeparam>
/// <typeparam name="TResponse">The type of the response.</typeparam>
public class PipeDescriptor<TRequest, TResponse>() : PipeDescriptor(typeof(TRequest), typeof(TResponse))
    where TRequest : class, IRequest<TResponse>
    where TResponse : class;
    