using conduit.Exceptions;
using conduit.logging;

namespace conduit.Pipes;

public class PipeFactory(IServiceProvider serviceProvider, ILog logger, IPipeConfigurationCache pipeCache) : IPipeFactory
{
    /// <inheritdoc />
    public IPipe<TRequest, TResponse> Create<TRequest, TResponse>() 
        where TRequest : class, IRequest<TResponse> 
        where TResponse : class
    {
        var config = pipeCache.Get<TRequest, TResponse>();
        if (config is null) throw new PipeNotFoundException();
        
        var stages = config.Stages.Select(s => s.ServiceType).ToArray(); 
        var pipe = new BuildablePipe<TRequest, TResponse>(logger, serviceProvider, stages);
        return pipe;
    }
}