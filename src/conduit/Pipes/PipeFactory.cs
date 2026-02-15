using conduit.Exceptions;
using conduit.logging;

namespace conduit.Pipes;

/// <summary>
/// Provides a concrete implementation of <see cref="IPipeFactory"/> for creating pipe instances.
/// </summary>
/// <param name="serviceProvider">The service provider for resolving pipe stages.</param>
/// <param name="logger">The logger instance for debugging.</param>
/// <param name="pipeCache">The cache containing pipe configurations.</param>
public class PipeFactory(
    IServiceProvider serviceProvider, 
    ILog logger, 
    IPipeConfigurationCache pipeCache) : IPipeFactory
{
    /// <inheritdoc/>
    public IPipe<TRequest, TResponse> Create<TRequest, TResponse>() 
        where TRequest : class, IRequest<TResponse> 
        where TResponse : class
    {
        var config = pipeCache.Get<TRequest, TResponse>();
        if (config is null) throw new PipeNotFoundException();
        
        var stages = config.Stages.Select(s => s.InterfaceType).ToArray(); 
        var pipe = new BuildablePipe<TRequest, TResponse>(logger, serviceProvider, stages);
        return pipe;
    }

    /// <inheritdoc/>
    public IPipe Create(Type requestType, Type responseType)
    {
        var config = pipeCache.Get(requestType, responseType);
        if (config is null) throw new PipeNotFoundException();
        
        var stages = config.Stages.Select(s => s.InterfaceType).ToArray();
        var type = typeof(BuildablePipe<,>).MakeGenericType(requestType, responseType);
        var arguments = new object[] { logger, serviceProvider, stages };
        return (Activator.CreateInstance(type, arguments) as IPipe)!;
    }
}