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
public sealed class DefaultPipeConfiguration<TRequest, TResponse, THandler>(DefaultPipeConfiguration configuration)
    where TRequest : class, IRequest<TResponse> 
    where TResponse : class
    where THandler : IRequestHandler<TRequest, TResponse>
{
    /// <summary>
    /// Gets the complete list of stages for this pipe configuration, including pre-execution, handler, and post-execution stages.
    /// </summary>
    /// <returns>An array of stage types in execution order.</returns>
    public Type[] GetStages()
    {
        var all = new List<Type>();
        all.AddRange(configuration.PreExecutionStages.Select(MaterializeTypes));
        all.Add(typeof(THandler));
        all.AddRange(configuration.PostExecutionStages.Select(MaterializeTypes));
        return all.ToArray();
    }

    private Type MaterializeTypes(Type incoming)
    {
        if (!incoming.IsGenericTypeDefinition) return incoming;
        var countParameters = incoming.GetGenericArguments().Length;
        if (countParameters < 3)
        {
            return incoming.MakeGenericType(typeof(TRequest), typeof(TResponse));
        }
        
        return incoming.MakeGenericType(typeof(TRequest), typeof(TResponse), typeof(THandler));
    }
}