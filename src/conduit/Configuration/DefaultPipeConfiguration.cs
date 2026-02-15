namespace conduit.Configuration;

public class DefaultPipeConfiguration
{
    public List<Type> PreExecutionStages { get; } = new();
    public List<Type> PostExecutionStages { get; } = new();
    
    public void AddPreExecutionStage(Type stage) => PreExecutionStages.Add(stage);
    public void AddPostExecutionStage(Type stage) => PostExecutionStages.Add(stage);
}


public sealed class DefaultPipeConfiguration<TRequest, TResponse, THandler>(DefaultPipeConfiguration configuration)
    where TRequest : class, IRequest<TResponse> 
    where TResponse : class
    where THandler : IRequestHandler<TRequest, TResponse>
{
    

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