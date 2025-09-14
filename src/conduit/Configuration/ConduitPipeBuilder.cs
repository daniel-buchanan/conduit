using conduit.Pipes;
using Microsoft.Extensions.DependencyInjection;

namespace conduit.Configuration;

/// <summary>
/// Builds a conduit pipe for a specific request and response type.
/// </summary>
/// <typeparam name="TRequest">The type of the request.</typeparam>
/// <typeparam name="TResponse">The type of the response.</typeparam>
public class ConduitPipeBuilder<TRequest, TResponse>(List<ServiceDescriptor> descriptors) 
    : IConduitPipeBuilder<TRequest, TResponse> 
    where TRequest : class, IRequest<TResponse>
    where TResponse : class
{
    /// <summary>
    /// Adds a request handler to the current pipe.
    /// </summary>
    /// <typeparam name="THandler">The type of the request handler to add.</typeparam>
    /// <returns>The current pipe builder instance.</returns>
    public IConduitPipeBuilder<TRequest, TResponse> AddHandler<THandler>() 
        where THandler : IRequestHandler<TRequest, TResponse>
    {
        var defs = CondiutConfigurationBuilder.GetHandlerDescriptors<TRequest, TResponse, THandler>();
        descriptors.AddRange(defs);
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
        descriptors.Add(new ServiceDescriptor(typeof(TStage), typeof(TStage), ServiceLifetime.Scoped));
        return this;
    }
}