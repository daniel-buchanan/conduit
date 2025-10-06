using conduit.Pipes;
using Microsoft.Extensions.DependencyInjection;

namespace conduit.Configuration;

/// <summary>
/// Builds a conduit pipe for a specific request and response type.
/// </summary>
/// <typeparam name="TRequest">The type of the request.</typeparam>
/// <typeparam name="TResponse">The type of the response.</typeparam>
public class ConduitPipeBuilder<TRequest, TResponse> 
    : IConduitPipeBuilder<TRequest, TResponse> 
    where TRequest : class, IRequest<TResponse>
    where TResponse : class
{
    private readonly PipeDescriptor<TRequest, TResponse> _descriptor = new();
    
    /// <summary>
    /// Adds a request handler to the current pipe.
    /// </summary>
    /// <typeparam name="THandler">The type of the request handler to add.</typeparam>
    /// <returns>The current pipe builder instance.</returns>
    public IConduitPipeBuilder<TRequest, TResponse> AddHandler<THandler>() 
        where THandler : IRequestHandler<TRequest, TResponse>
    {
        var handlerDef = new ServiceDescriptor(typeof(IRequestHandler<TRequest, TResponse>), typeof(THandler), ServiceLifetime.Scoped);
        _descriptor.Stages.Add(handlerDef);
        return this;
    }

    /// <summary>
    /// Adds a pipe stage to the current pipe.
    /// </summary>
    /// <typeparam name="TStage">The type of the pipe stage to add.</typeparam>
    /// <returns>The current pipe builder instance.</returns>
    public IConduitPipeBuilder<TRequest, TResponse> AddStage<TStage>() 
        where TStage : IPipeStage<TRequest, TResponse>
    {
        _descriptor.Stages.Add(new ServiceDescriptor(typeof(TStage), typeof(TStage), ServiceLifetime.Scoped));
        return this;
    }
    
    public PipeDescriptor GetDescriptor() => _descriptor;
}