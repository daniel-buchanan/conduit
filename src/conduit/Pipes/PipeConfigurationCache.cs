using conduit.common;
using conduit.Configuration;
using conduit.Exceptions;

namespace conduit.Pipes;

public class PipeConfigurationCache(IHashUtil hashUtil) : IPipeConfigurationCache
{
    private readonly Dictionary<string, PipeDescriptor> _cache = new();
    private bool _isLocked;

    /// <inheritdoc />
    public void Lock()
        => _isLocked = true;

    /// <inheritdoc />
    public void Add<TRequest, TResponse>(PipeDescriptor descriptor) 
        where TRequest : class, IRequest<TResponse> 
        where TResponse : class
    {
        if (_isLocked) return;
        
        if (_cache.ContainsKey(descriptor.GetHash()))
            throw new PipeAlreadyRegisteredException();
        
        _cache.Add(descriptor.GetHash(), descriptor);
    }

    /// <inheritdoc />
    public PipeDescriptor? Get<TRequest, TResponse>() 
        where TRequest : class, IRequest<TResponse> 
        where TResponse : class
    {
        var key = hashUtil.TypeNameHash<TRequest, TResponse>();
        return _cache.TryGetValue(key, out var descriptor) ? descriptor : null;
    }
}