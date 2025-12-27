using conduit.Configuration;
using conduit.logging;
using conduit.Pipes.Stages;

namespace conduit.Pipes;

/// <summary>
/// Represents the default pipe implementation in the Conduit system.
/// This pipe orchestrates the execution of pre-execution, request handling, and post-execution stages.
/// </summary>
/// <typeparam name="TRequest">The type of the request handled by this pipe.</typeparam>
/// <typeparam name="TResponse">The type of the response produced by this pipe.</typeparam>
public class DefaultPipe<TRequest, TResponse> : BuildablePipe<TRequest, TResponse>
    where TRequest : class, IRequest<TResponse>
    where TResponse : class 
{
    /// <summary>
    /// Represents the default pipe implementation in the Conduit system.
    /// This pipe orchestrates the execution of pre-execution, request handling, and post-execution stages.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request handled by this pipe.</typeparam>
    /// <typeparam name="TResponse">The type of the response produced by this pipe.</typeparam>
    /// <param name="logger">The logger to use for this pipe.</param>
    /// <param name="provider">The IServiceProvider to use for retrieving stages.</param>
    public DefaultPipe(
        ILog logger, 
        IServiceProvider provider, 
        DefaultPipeConfiguration<TRequest, TResponse, IRequestHandler<TRequest, TResponse>> config) : 
        base(logger, provider,config.GetStages()) { }
}