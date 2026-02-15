using conduit.Configuration;
using conduit.Exceptions;

namespace conduit.Pipes;

/// <summary>
/// Defines the contract for a cache that stores pipe configurations.
/// </summary>
public interface IPipeConfigurationCache
{
    /// <summary>
    /// Locks the cache to prevent further modifications.
    /// </summary>
    void Lock();
    
    /// <summary>
    /// Adds a new pipe configuration to the cache.
    /// </summary>
    /// <param name="descriptor">The descriptor to add.</param>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <exception cref="PipeAlreadyRegisteredException">
    /// Thrown if a pipe has already been registered for the request and response types.
    /// </exception>
    void Add<TRequest, TResponse>(PipeDescriptor descriptor)
        where TRequest : class, IRequest<TResponse>
        where TResponse : class;
    
    /// <summary>
    /// Gets a pipe configuration from the cache.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <returns>The pipe descriptor if found, otherwise null.</returns>
    PipeDescriptor? Get<TRequest, TResponse>()
        where TRequest : class, IRequest<TResponse>
        where TResponse : class;
    
    /// <summary>
    /// Gets a pipe configuration from the cache.
    /// </summary>
    /// <param name="requestType">The request type.</param>
    /// <param name="responseType">The response type.</param>
    /// <returns>The pipe descriptor if found, otherwise null.</returns>
    PipeDescriptor? Get(Type requestType, Type responseType);
}