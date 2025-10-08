using conduit.logging;

namespace conduit.Pipes;

public class BuildablePipe<TRequest, TResponse> : Pipe<TRequest, TResponse> 
    where TResponse : class 
    where TRequest : class, IRequest<TResponse>
{
    private readonly ILog _logger;
    private readonly Type[] _stages;
    private readonly IServiceProvider _serviceProvider;
    
    public BuildablePipe(ILog logger, IServiceProvider serviceProvider, Type[] stages)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _stages = stages;
    }
    
    public override async Task<TResponse?> SendAsync(TRequest request, CancellationToken cancellationToken = default)
    {
        TResponse? response = null;
        var instanceId = Guid.NewGuid();
        foreach (var s in _stages)
        {
            var stage = (IPipeStage<TRequest, TResponse>?)_serviceProvider.GetService(s);
            if (stage == null)
                throw new InvalidOperationException($"Could not resolve stage of type {s.Name}");
            
            _logger.Debug($"[{instanceId}] Executing stage {s.Name}");
            var currentResponse = await stage.ExecuteAsync(instanceId, request, cancellationToken);
            if (response == null) response = currentResponse;
        }
        
        return response;
    }
}