using conduit.common;
using conduit.Helpers;
using conduit.Pipes;
using conduit.Pipes.Stages;
using Microsoft.Extensions.DependencyInjection;

namespace conduit.Configuration;

/// <summary>
/// Provides a concrete implementation of <see cref="IConduitConfigurationBuilder"/> for configuring the Conduit system.
/// </summary>
public class ConduitConfigurationBuilder : IConduitConfigurationBuilder
{
    private readonly DefaultPipeConfiguration _defaultPipeConfiguration = new();
    private readonly List<ServiceDescriptor> _descriptors = new();
    private readonly IPipeConfigurationRegistry _pipeConfigurationRegistry = new PipeConfigurationRegistry(HashUtil.Instance);

    /// <summary>
    /// Initializes a new instance of the <see cref="ConduitConfigurationBuilder"/> class.
    /// Wires in the default debug pre-/post-execution stages only when the log level is Debug — at any
    /// other level the logger already suppresses their output, so running them would only add
    /// pipe-execution overhead for a stage that writes nothing.
    /// </summary>
    /// <param name="environment">
    /// The environment used to read the configured log level. Defaults to <see cref="EnvironmentImpl"/>
    /// (reads the <c>LOG_LEVEL</c> environment variable) when not supplied.
    /// </param>
    public ConduitConfigurationBuilder(IEnvironment? environment = null)
    {
        environment ??= new EnvironmentImpl();
        if (environment.LogLevel != LoggingLevel.Debug) return;

        AddDefaultPreExecutionStage(typeof(DebugPreExecutionStage<,>));
        AddDefaultPostExecutionStage(typeof(DebugPostExecutionStage<,>));

        // Registered as open generics, the same way AddValidation registers ValidationStage<,>, so DI can
        // construct a debug stage for ANY (TRequest, TResponse) pair on demand — including pipes built via
        // RegisterPipe, which (unlike RegisterHandler's GetServiceDescriptorsForHandler) never registers a
        // closed DebugPreExecutionStage/DebugPostExecutionStage for its own pair.
        AddDescriptor(new ServiceDescriptor(typeof(DebugPreExecutionStage<,>), typeof(DebugPreExecutionStage<,>), ServiceLifetime.Transient));
        AddDescriptor(new ServiceDescriptor(typeof(DebugPostExecutionStage<,>), typeof(DebugPostExecutionStage<,>), ServiceLifetime.Transient));
    }

    /// <summary>
    /// Builds the Conduit configuration and registers all configured services.
    /// </summary>
    /// <param name="services">The service collection to add configured services to.</param>
    public void Build(IServiceCollection services)
    {
        services.AddRange(_descriptors.ToArray());
        services.AddSingleton(_defaultPipeConfiguration);

        _pipeConfigurationRegistry.Lock();
        services.AddSingleton(_pipeConfigurationRegistry);
    }

    /// <summary>
    /// Adds a service descriptor to the configuration.
    /// </summary>
    /// <param name="descriptor">The service descriptor to add.</param>
    public void AddDescriptor(ServiceDescriptor descriptor)
        => _descriptors.Add(descriptor);

    private void AddDescriptors(params ServiceDescriptor[] descriptors)
        => _descriptors.AddRange(descriptors);

    /// <summary>
    /// Adds a pre-execution stage to be applied to all pipes by default.
    /// </summary>
    /// <param name="stage">The type of the pre-execution stage.</param>
    public void AddDefaultPreExecutionStage(Type stage)
        => _defaultPipeConfiguration.PreExecutionStages.Add(stage);

    /// <summary>
    /// Adds a post-execution stage to be applied to all pipes by default.
    /// </summary>
    /// <param name="stage">The type of the post-execution stage.</param>
    public void AddDefaultPostExecutionStage(Type stage)
        => _defaultPipeConfiguration.PostExecutionStages.Add(stage);

    /// <inheritdoc/>
    public IConduitConfigurationBuilder RegisterHandler<TRequest, TResponse, THandler>(Action<IHandlerRegistrationOptions>? configure = null)
        where TRequest : class, IRequest<TResponse>
        where TResponse : class
        where THandler : IRequestHandler<TRequest, TResponse>
    {
        var options = new HandlerRegistrationOptions();
        configure?.Invoke(options);

        _pipeConfigurationRegistry.Add<TRequest, TResponse>(new PipeDescriptor<TRequest, TResponse>());

        var defs = ReflectionHelper.GetServiceDescriptorsForHandler<TRequest, TResponse, THandler>(options.IsValidationExcluded);
        AddDescriptors(defs);
        return this;
    }

    /// <inheritdoc/>
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

            var defs = ReflectionHelper.GetServiceDescriptorsForHandler(request, response, t);
            AddDescriptors(defs);
        }

        return this;
    }

    /// <inheritdoc/>
    public IConduitConfigurationBuilder RegisterPipe<TRequest, TResponse>(Action<IConduitPipeBuilder<TRequest, TResponse>> configure)
        where TRequest : class, IRequest<TResponse>
        where TResponse : class
    {
        var builder = new ConduitPipeBuilder<TRequest, TResponse>();
        configure(builder);

        var pipeDef = builder.GetDescriptor();

        AddDescriptors(pipeDef.Stages.Select(s => s.Descriptor).ToArray());

        var pipeServiceDescriptor = GetConfiguredPipeDescriptor<TRequest, TResponse>();
        _descriptors.Add(pipeServiceDescriptor);

        _pipeConfigurationRegistry.Add<TRequest, TResponse>(pipeDef);

        return this;
    }

    private static ServiceDescriptor GetConfiguredPipeDescriptor<TRequest, TResponse>()
        where TRequest : class, IRequest<TResponse>
        where TResponse : class
        => new (typeof(IPipe<TRequest, TResponse>), PipeFactory<TRequest, TResponse>, ServiceLifetime.Scoped);

    private static IPipe<TRequest, TResponse> PipeFactory<TRequest, TResponse>(IServiceProvider provider)
        where TRequest : class, IRequest<TResponse>
        where TResponse : class
    {
        var factory = provider.GetRequiredService<IPipeFactory>();
        return factory.Create<TRequest, TResponse>();
    }

    /// <inheritdoc/>
    public IConduitConfigurationBuilder RegisterPipesAsImplementedFrom<TLocator>()
    {
        var descriptors = ReflectionHelper.GetRegistrationsAsImplementedFrom<TLocator, IPipe>();
        AddDescriptors(descriptors);
        return this;
    }
}
