using conduit.common;
using conduit.Pipes;

namespace conduit.Configuration;

/// <summary>
/// Represents the configuration for the Conduit system, managing pipe stages and their descriptors.
/// </summary>
/// <param name="hash">The utility for hashing type names.</param>
public class ConduitConfiguration(IHashUtil hash) : IConduitConfiguration
{
    private readonly Dictionary<string, List<StageDescriptor>> _pipes = new();
    private readonly Dictionary<string, PipeDescriptor> _pipeDescriptors = new();

    /// <inheritdoc/>
    public IEnumerable<PipeDescriptor> GetPipes()
    {
        foreach (var p in _pipeDescriptors)
        {
            p.Value.SetStages(_pipes[p.Key]);
            yield return p.Value;
        }
    }
    
    /// <inheritdoc/>
    public void AddPipeStage<TStage, TRequest, TResponse>()
        where TStage : IPipeStage<TRequest, TResponse>
        where TRequest : class, IRequest<TResponse>
        where TResponse : class
        => AddPipeStage(typeof(TRequest), typeof(TResponse), typeof(TStage));

    /// <inheritdoc/>
    public void AddPipeStage(Type requestType, Type responseType, Type implementationType, Type? interfaceType = null)
    {
        var typeHash = hash.TypeNameHash(requestType);
        var existing = _pipes.ContainsKey(typeHash);
        if (!existing)
        {
            _pipes.Add(typeHash, []);
            _pipeDescriptors.Add(typeHash, new PipeDescriptor(requestType, responseType));
        }
        
        var stage = new StageDescriptor(requestType, responseType, implementationType, interfaceType);
        _pipes[typeHash].Add(stage);
    }
}