using conduit.common;
using conduit.Configuration;
using conduit.Exceptions;

namespace conduit.Pipes;

/// <summary>
/// Provides a concrete implementation of a registry that stores pipe configurations.
/// </summary>
/// <param name="hashUtil">The hash utility for generating registry keys.</param>
public class PipeConfigurationRegistry(IHashUtil hashUtil) : IPipeConfigurationRegistry
{
    private readonly Dictionary<string, PipeDescriptor> _registry = new();
    private bool _isLocked;

    /// <summary>
    /// Locks the registry to prevent further additions.
    /// </summary>
    public void Lock()
        => _isLocked = true;

    /// <summary>
    /// Adds a pipe descriptor to the registry.
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

        if (_registry.ContainsKey(descriptor.GetHash()))
            throw new PipeAlreadyRegisteredException();

        _registry.Add(descriptor.GetHash(), descriptor);
    }

    /// <summary>
    /// Retrieves a pipe descriptor from the registry for the specified request and response types.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <returns>The registered pipe descriptor, or null if not found.</returns>
    public PipeDescriptor? Get<TRequest, TResponse>()
        where TRequest : class, IRequest<TResponse>
        where TResponse : class
        => Get(typeof(TRequest), typeof(TResponse));

    /// <summary>
    /// Retrieves a pipe descriptor from the registry for the specified request and response types.
    /// </summary>
    /// <param name="requestType">The request type.</param>
    /// <param name="responseType">The response type.</param>
    /// <returns>The registered pipe descriptor, or null if not found.</returns>
    public PipeDescriptor? Get(Type requestType, Type responseType)
    {
        var key = hashUtil.TypeNameHash(requestType, responseType);
        return _registry.GetValueOrDefault(key);
    }
}
