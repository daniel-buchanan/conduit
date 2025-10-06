using conduit.Configuration;
using conduit.Exceptions;

namespace conduit.Pipes;

public interface IPipeConfigurationCache
{
    /// <summary>
    /// Lock the cache to prevent further modifications.
    /// </summary>
    void Lock();
    
    /// <summary>
    /// Add a new pipe configuration to the cache.
    /// </summary>
    /// <param name="descriptor">The descriptor to add.</param>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <exception cref="PipeAlreadyRegisteredException">
    /// If a pipe has already been registered for the request and response types, this exception will be thrown.
    /// </exception>
    void Add<TRequest, TResponse>(PipeDescriptor descriptor)
        where TRequest : class, IRequest<TResponse>
        where TResponse : class;
    
    /// <summary>
    /// Get a pipe configuration from the cache.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <returns>The pipe descriptor if found, otherwise null.</returns>
    PipeDescriptor? Get<TRequest, TResponse>()
        where TRequest : class, IRequest<TResponse>
        where TResponse : class;
}