using conduit.common;
using conduit.Configuration;
using conduit.Exceptions;

namespace conduit.Pipes;

/// <summary>
/// Provides a concrete implementation of a cache that stores pipe configurations.
/// </summary>
/// <param name="hashUtil">The hash utility for generating cache keys.</param>
public class PipeConfigurationCache(IHashUtil hashUtil) : IPipeConfigurationCache
{
    private readonly Dictionary<string, PipeDescriptor> _cache = new();
    private bool _isLocked;

    /// <summary>
    /// Locks the cache to prevent further additions.
    /// </summary>
    public void Lock()
        => _isLocked = true;

    /// <summary>
    /// Adds a pipe descriptor to the cache.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="descriptor">The pipe descriptor to add.</param>
    /// <exception cref="PipeAlreadyRegisteredException">Thrown if a pipe with the same request and response types is already registered.</exception>
    public void Add<TRequest, TResponse>(PipeDescriptor descriptor) 
        where TRequest : class, IRequest<TResponse> 
        where TResponse : class
    {
        if (_isLocked) return;
        
        if (_cache.ContainsKey(descriptor.GetHash()))
            throw new PipeAlreadyRegisteredException();
        
        _cache.Add(descriptor.GetHash(), descriptor);
    }

    /// <summary>
    /// Retrieves a pipe descriptor from the cache for the specified request and response types.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <returns>The cached pipe descriptor, or null if not found.</returns>
    public PipeDescriptor? Get<TRequest, TResponse>() 
        where TRequest : class, IRequest<TResponse> 
        where TResponse : class 
        => Get(typeof(TRequest), typeof(TResponse));

    /// <summary>
    /// Retrieves a pipe descriptor from the cache for the specified request and response types.
    /// </summary>
    /// <param name="requestType">The request type.</param>
    /// <param name="responseType">The response type.</param>
    /// <returns>The cached pipe descriptor, or null if not found.</returns>
    public PipeDescriptor? Get(Type requestType, Type responseType)
    {
        var key = hashUtil.TypeNameHash(requestType, responseType);
        return _cache.GetValueOrDefault(key);
    }
}