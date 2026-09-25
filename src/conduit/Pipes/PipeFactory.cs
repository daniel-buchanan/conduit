using conduit.Configuration;
using conduit.Exceptions;
using conduit.Helpers;
using conduit.logging;
using Microsoft.Extensions.DependencyInjection;

namespace conduit.Pipes;

/// <summary>
/// Provides a concrete implementation of <see cref="IPipeFactory"/> for creating pipe instances.
/// </summary>
/// <param name="serviceProvider">The service provider for resolving pipe stages.</param>
/// <param name="logger">The logger instance for debugging.</param>
/// <param name="pipeConfigurationRegistry">The registry containing pipe configurations.</param>
public class PipeFactory(
    IServiceProvider serviceProvider,
    ILog logger,
    IPipeConfigurationRegistry pipeConfigurationRegistry) : IPipeFactory
{
    /// <inheritdoc/>
    public IPipe<TRequest, TResponse> Create<TRequest, TResponse>()
        where TRequest : class, IRequest<TResponse>
        where TResponse : class
    {
        var config = pipeConfigurationRegistry.Get<TRequest, TResponse>();
        if (config is null) throw new PipeNotFoundException();

        var stages = BuildStages(config, typeof(TRequest), typeof(TResponse));
        var pipe = new BuildablePipe<TRequest, TResponse>(logger, serviceProvider, stages);
        return pipe;
    }

    /// <summary>
    /// Builds the final stage-type array for a pipe, sandwiching the pipe's explicitly configured stages
    /// between the globally registered default pre- and post-execution stages, skipping any default stage
    /// that implements <see cref="IValidationPipeStage"/> when the pipe is excluded from validation.
    /// </summary>
    private Type[] BuildStages(PipeDescriptor config, Type requestType, Type responseType)
    {
        var defaults = serviceProvider.GetRequiredService<DefaultPipeConfiguration>();

        var preStages = MaterializeDefaultStages(defaults.PreExecutionStages, config.ExcludeValidation, requestType, responseType);
        var explicitStages = config.Stages.Select(s => s.InterfaceType);
        var postStages = MaterializeDefaultStages(defaults.PostExecutionStages, config.ExcludeValidation, requestType, responseType);

        return preStages.Concat(explicitStages).Concat(postStages).ToArray();
    }

    private static IEnumerable<Type> MaterializeDefaultStages(
        IEnumerable<Type> stageTypes, bool excludeValidation, Type requestType, Type responseType)
        => stageTypes
            .Where(t => StageMaterializer.IsIncluded(t, excludeValidation))
            .Select(t => StageMaterializer.Materialize(t, requestType, responseType));
}