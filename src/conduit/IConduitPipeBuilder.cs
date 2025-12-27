using conduit.Pipes;

namespace conduit;

/// <summary>
/// Defines the contract for a builder that configures a specific Conduit pipe.
/// </summary>
/// <typeparam name="TRequest">The type of the request.</typeparam>
/// <typeparam name="TResponse">The type of the response.</typeparam>
public interface IConduitPipeBuilder<TRequest, TResponse>
    where TResponse : class
    where TRequest : class, IRequest<TResponse>
{
    /// <summary>
    /// Adds a pipe stage to the current pipe.
    /// </summary>
    /// <typeparam name="TStage">The type of the pipe stage to add.</typeparam>
    /// <returns>The current pipe builder instance.</returns>
    IConduitPipeBuilder<TRequest, TResponse> AddStage<TStage>()
        where TStage : IPipeStage<TRequest, TResponse>;

    /// <summary>
    /// Adds a pipe stage to the current pipe.
    /// </summary>
    /// <returns>The current pipe builder instance.</returns>
    IConduitPipeBuilder<TRequest, TResponse> AddStage(Type stage);
    
    /// <summary>
    /// Adds a request handler to the current pipe.
    /// </summary>
    /// <typeparam name="THandler">The type of the request handler to add.</typeparam>
    /// <returns>The current pipe builder instance.</returns>
    IConduitPipeBuilder<TRequest, TResponse> AddHandler<THandler>()
        where THandler : IRequestHandler<TRequest, TResponse>;
}