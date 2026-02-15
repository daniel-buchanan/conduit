using conduit.Exceptions;
using conduit.logging;

namespace conduit.Pipes;

public class PipeFactory(
    IServiceProvider serviceProvider, 
    ILog logger, 
    IPipeConfigurationCache pipeCache) : IPipeFactory
{
    /// <inheritdoc />
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