using conduit.Helpers;
using conduit.Pipes;

namespace conduit.Configuration;

/// <summary>
/// Represents the default configuration for Conduit pipes, including pre- and post-execution stages.
/// </summary>
public class DefaultPipeConfiguration
{
    /// <summary>
    /// Gets the list of pre-execution stages that are applied to all pipes by default.
    /// </summary>
    public List<Type> PreExecutionStages { get; } = new();
    
    /// <summary>
    /// Gets the list of post-execution stages that are applied to all pipes by default.
    /// </summary>
    public List<Type> PostExecutionStages { get; } = new();
    
    /// <summary>
    /// Adds a pre-execution stage to the default configuration.
    /// </summary>
    /// <param name="stage">The type of the stage to add.</param>
    public void AddPreExecutionStage(Type stage) => PreExecutionStages.Add(stage);
    
    /// <summary>
    /// Adds a post-execution stage to the default configuration.
    /// </summary>
    /// <param name="stage">The type of the stage to add.</param>
    public void AddPostExecutionStage(Type stage) => PostExecutionStages.Add(stage);
}


/// <summary>
/// Represents the strongly-typed default configuration for a specific pipe with request and response types.
/// </summary>
/// <typeparam name="TRequest">The type of the request.</typeparam>
/// <typeparam name="TResponse">The type of the response.</typeparam>
/// <typeparam name="THandler">The type of the request handler.</typeparam>
/// <param name="configuration">The shared default pre/post-execution stage configuration.</param>
/// <param name="excludeValidation">Whether to skip default stages that implement <see cref="IValidationPipeStage"/>.</param>
public sealed class DefaultPipeConfiguration<TRequest, TResponse, THandler>(DefaultPipeConfiguration configuration, bool excludeValidation = false)
    where TRequest : class, IRequest<TResponse>
    where TResponse : class
    where THandler : IRequestHandler<TRequest, TResponse>
{
    /// <summary>
    /// Gets the complete list of stages for this pipe configuration, including pre-execution, handler, and post-execution stages.
    /// Skips any default stage that implements <see cref="IValidationPipeStage"/> when this registration excludes validation.
    /// </summary>
    /// <returns>An array of stage types in execution order.</returns>
    public Type[] GetStages()
    {
        var all = new List<Type>();
        all.AddRange(configuration.PreExecutionStages.Where(IsIncluded).Select(MaterializeTypes));
        all.Add(typeof(THandler));
        all.AddRange(configuration.PostExecutionStages.Where(IsIncluded).Select(MaterializeTypes));
        return all.ToArray();
    }

    private bool IsIncluded(Type stageType)
        => StageMaterializer.IsIncluded(stageType, excludeValidation);

    private Type MaterializeTypes(Type incoming)
        => StageMaterializer.Materialize(incoming, typeof(TRequest), typeof(TResponse), typeof(THandler));
}