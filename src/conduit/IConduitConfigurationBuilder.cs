namespace conduit;

/// <summary>
/// Defines the contract for a builder that configures the Conduit system.
/// </summary>
public interface IConduitConfigurationBuilder
{
    /// <summary>
    /// Registers a request handler with the Conduit system.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request.</typeparam>
    /// <typeparam name="TResponse">The type of the response.</typeparam>
    /// <typeparam name="THandler">The interface type of the handler.</typeparam>
    /// <returns>The current configuration builder instance.</returns>
    IConduitConfigurationBuilder RegisterHandler<TRequest, TResponse, THandler>()
        where TRequest : class, IRequest<TResponse>
        where TResponse : class
        where THandler : IRequestHandler<TRequest, TResponse>;

    /// <summary>
    /// Registers all request handlers found in the assembly of the specified locator type.
    /// </summary>
    /// <typeparam name="TLocator">A type from the assembly to scan for handlers.</typeparam>
    /// <returns>The current configuration builder instance.</returns>
    IConduitConfigurationBuilder RegisterHandlersAsImplementedFrom<TLocator>();
    
    /// <summary>
    /// Registers a custom pipe for a specific request and response type.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request.</typeparam>
    /// <typeparam name="TResponse">The type of the response.</typeparam>
    /// <param name="configure">An action to configure the pipe builder.</param>
    /// <returns>The current configuration builder instance.</returns>
    IConduitConfigurationBuilder RegisterPipe<TRequest, TResponse>(
        Action<IConduitPipeBuilder<TRequest, TResponse>> configure)
        where TRequest : class, IRequest<TResponse>
        where TResponse : class;
    
    /// <summary>
    /// Registers all pipes found in the assembly of the specified locator type.
    /// </summary>
    /// <typeparam name="TLocator">A type from the assembly to scan for pipes.</typeparam>
    /// <returns>The current configuration builder instance.</returns>
    IConduitConfigurationBuilder RegisterPipesAsImplementedFrom<TLocator>();
}