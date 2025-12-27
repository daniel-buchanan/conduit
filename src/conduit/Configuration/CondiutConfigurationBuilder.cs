using conduit.common;
using conduit.common.Helpers;
using conduit.Pipes;
using Microsoft.Extensions.DependencyInjection;

namespace conduit.Configuration;

public class ConduitConfigurationBuilder(ConduitConfiguration config) : IConduitConfigurationBuilder
{
    private readonly List<ServiceDescriptor> _descriptors = new();
    private readonly IPipeConfigurationCache _pipeConfigurationCache = new PipeConfigurationCache(HashUtil.Instance);

    /// <summary>
    /// Builds the Conduit configuration and registers it as a singleton service.
    /// </summary>
    /// <param name="services">The service collection to add to.</param>
    /// <returns>The configured Conduit configuration.</returns>
    public IConduitConfiguration Build(IServiceCollection services)
    {
        var distinctDescriptors = _descriptors.Distinct().ToArray();
        services.AddRange(distinctDescriptors);
        
        _pipeConfigurationCache.Lock();
        services.AddSingleton(_pipeConfigurationCache);
        
        return config;
    }
    
    public void AddDescriptor(ServiceDescriptor descriptor)
        => _descriptors.Add(descriptor);
    
    public void AddDescriptor(Type serviceType, Type implementationType, ServiceLifetime lifetime)
        => _descriptors.Add(new ServiceDescriptor(serviceType, implementationType, lifetime));

    /// <inheritdoc />
    public IConduitConfigurationBuilder RegisterHandler<TRequest, TResponse, THandler>() 
        where TRequest : class, IRequest<TResponse>
        where TResponse : class
        where THandler : IRequestHandler<TRequest, TResponse>
    {
        var defs = ReflectionHelper.GetServiceDescriptorsForHandler<TRequest, TResponse, THandler>();
        _descriptors.AddRange(defs);
        return this;
    }

    /// <inheritdoc />
    public IConduitConfigurationBuilder RegisterHandlersAsImplementedFrom<TLocator>()
    {
        var types = ReflectionHelper.GetTypesFromAssembly<TLocator>(t => t == typeof(IRequestHandler));
        foreach (var t in types)
        {
            var genericParameters = t.GetGenericArguments();
            if (genericParameters.Length == 0 && t.BaseType != null) genericParameters = t.BaseType.GetGenericArguments();
            if (genericParameters.Length == 0) continue;
            
            var request = genericParameters[0];
            var response = genericParameters[1];
            
            _descriptors.AddRange(ReflectionHelper.GetServiceDescriptorsForHandler(request, response, t));
        }
        
        return this;
    }

    /// <inheritdoc />
    public IConduitConfigurationBuilder RegisterPipe<TRequest, TResponse>(Action<IConduitPipeBuilder<TRequest, TResponse>> configure) where TRequest : class, IRequest<TResponse> where TResponse : class
    {
        var builder = new ConduitPipeBuilder<TRequest, TResponse>();
        configure(builder);
        var pipeDef = builder.GetDescriptor();
        _descriptors.AddRange(pipeDef.Stages.Select(s => s.Descriptor));
        var pipeServiceDescriptor = GetConfiguredPipeDescriptor<TRequest, TResponse>();
        _descriptors.Add(pipeServiceDescriptor);
        _pipeConfigurationCache.Add<TRequest, TResponse>(pipeDef);
        return this;
    }
    
    private static ServiceDescriptor GetConfiguredPipeDescriptor<TRequest, TResponse>()
        where TRequest : class, IRequest<TResponse>
        where TResponse : class
        => new ServiceDescriptor(typeof(IPipe<TRequest, TResponse>), PipeFactory<TRequest, TResponse>, ServiceLifetime.Scoped);
    
    private static IPipe<TRequest, TResponse> PipeFactory<TRequest, TResponse>(IServiceProvider provider)
        where TRequest : class, IRequest<TResponse>
        where TResponse : class
    {
        var factory = provider.GetRequiredService<IPipeFactory>();
        return factory.Create<TRequest, TResponse>();
    }

    /// <inheritdoc />
    public IConduitConfigurationBuilder RegisterPipesAsImplementedFrom<TLocator>()
    {
        var descriptors = ReflectionHelper.GetRegistrationsAsImplementedFrom<TLocator, IPipe>();
        _descriptors.AddRange(descriptors);

        return this;
    }
}