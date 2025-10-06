using conduit.common;
using conduit.Pipes;
using conduit.Pipes.Stages;
using Microsoft.Extensions.DependencyInjection;

namespace conduit.Configuration;

public class CondiutConfigurationBuilder(ConduitConfiguration config) : IConduitConfigurationBuilder
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

    public IConduitConfigurationBuilder RegisterHandler<TRequest, TResponse, THandler>() 
        where TRequest : class, IRequest<TResponse>
        where TResponse : class
        where THandler : IRequestHandler<TRequest, TResponse>
    {
        var defs = GetHandlerDescriptors<TRequest, TResponse, THandler>();
        _descriptors.AddRange(defs);
        return this;
    }

    public IConduitConfigurationBuilder RegisterHandlersAsImplementedFrom<TLocator>()
    {
        var assembly = typeof(TLocator).Assembly;
        var types = assembly.GetTypes();
        var handlerTypes = types.Where(t => t.GetInterfaces().Any(i => i == typeof(IRequestHandler) && !t.IsInterface));
        
        foreach (var handlerType in handlerTypes)
        {
            var genericParameters = handlerType.GetGenericArguments();
            if (genericParameters.Length == 0 && handlerType.BaseType != null)
                genericParameters = handlerType.BaseType.GetGenericArguments();

            if (genericParameters.Length == 0)
            {
                _descriptors.Add(new ServiceDescriptor(handlerType, handlerType, ServiceLifetime.Scoped));
                return this;
            }

            var defs = GetHandlerDescriptors(genericParameters[0], genericParameters[1], handlerType);
            _descriptors.AddRange(defs);
        }

        return this;
    }

    public IConduitConfigurationBuilder RegisterPipe<TRequest, TResponse>(Action<IConduitPipeBuilder<TRequest, TResponse>> configure) where TRequest : class, IRequest<TResponse> where TResponse : class
    {
        var builder = new ConduitPipeBuilder<TRequest, TResponse>();
        configure(builder);
        var pipeDef = builder.GetDescriptor();
        _descriptors.AddRange(pipeDef.Stages);
        var pipeServiceDescriptor = new ServiceDescriptor(typeof(IPipe<TRequest, TResponse>), provider =>
        {
            var factory = provider.GetRequiredService<IPipeFactory>();
            return factory.Create<TRequest, TResponse>();
        }, ServiceLifetime.Scoped);
        _descriptors.Add(pipeServiceDescriptor);
        _pipeConfigurationCache.Add<TRequest, TResponse>(pipeDef);
        return this;
    }

    public IConduitConfigurationBuilder RegisterPipesAsImplementedFrom<TLocator>()
    {
        var assembly = typeof(TLocator).Assembly;
        var types = assembly.GetTypes();
        var pipeTypes = types.Where(t => t.GetInterfaces().Any(i => i == typeof(IPipe) && !t.IsInterface));
        
        foreach (var pt in pipeTypes)
        {
            var genericParameters = pt.GetGenericArguments();
            if (genericParameters.Length == 0 && pt.BaseType != null)
                genericParameters = pt.BaseType.GetGenericArguments();

            if (genericParameters.Length == 0)
            {
                _descriptors.Add(new ServiceDescriptor(pt, pt, ServiceLifetime.Scoped));
                return this;
            }

            var ctors = pt.GetConstructors();
            var injectCtor = ctors.First(c => c.GetParameters().Length > 0);
            var ctorParameters = injectCtor.GetParameters().Select(p => p.ParameterType).ToArray();

            foreach (var ctorParam in ctorParameters)
            {
                if (ctorParam.IsInterface)
                {
                    var implementations = types.Where(t => t.GetInterfaces().Any(i => i == ctorParam));
                    var impl =  implementations.First();
                    _descriptors.Add(new ServiceDescriptor(ctorParam, impl, ServiceLifetime.Scoped));
                }
                else
                {
                    _descriptors.Add(new ServiceDescriptor(ctorParam, ctorParam, ServiceLifetime.Scoped));
                }
            }
        }

        return this;
    }

    internal static ServiceDescriptor[] GetHandlerDescriptors<TRequest, TResponse, THandler>()
        => GetHandlerDescriptors(typeof(TRequest), typeof(TResponse), typeof(THandler));

    internal static ServiceDescriptor[] GetHandlerDescriptors(Type request, Type response, Type handler)
    {
        var genericPipeInterface = typeof(IPipe<,>);
        var genericPipeImpl = typeof(DefaultPipe<,>);
        var genericHandlerStage = typeof(HandleRequestStage<,>);
        var genericPreStage = typeof(DebugPreExecutionStage<,>);
        var genericPostStage = typeof(DebugPostExecutionStage<,>);
        var genericHandlerInterface = typeof(IRequestHandler<,>);
        
        var pipeInterface = genericPipeInterface.MakeGenericType(request, response);
        var pipeImpl = genericPipeImpl.MakeGenericType(request, response);
        var handlerStage = genericHandlerStage.MakeGenericType(request, response);
        var preStage = genericPreStage.MakeGenericType(request, response);
        var postStage = genericPostStage.MakeGenericType(request, response);
        var handlerInterface = genericHandlerInterface.MakeGenericType(request, response);
        
        var pipeDescriptor = new ServiceDescriptor(pipeInterface, pipeImpl, ServiceLifetime.Scoped);
        var handlerStageDescriptor = new ServiceDescriptor(handlerStage, handlerStage, ServiceLifetime.Scoped);
        var preStageDescriptor = new  ServiceDescriptor(preStage, preStage, ServiceLifetime.Scoped);
        var postStageDescriptor = new ServiceDescriptor(postStage, postStage, ServiceLifetime.Scoped);
        var handlerDescriptor =  new ServiceDescriptor(handlerInterface, handler, ServiceLifetime.Scoped);
        
        return [pipeDescriptor, handlerStageDescriptor, preStageDescriptor, postStageDescriptor, handlerDescriptor];
    }
}