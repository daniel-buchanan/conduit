namespace conduit.Configuration;

public class DefaultPipeConfiguration<TRequest, TResponse, THandler>
    where TRequest : class, IRequest<TResponse> 
    where TResponse : class
    where THandler : IRequestHandler<TRequest, TResponse>
{
    public List<Type> PreExecutionStages { get; } = new();
    public List<Type> PostExecutionStages { get; } = new();
    
    public void AddPreExecutionStage(Type stage) => PreExecutionStages.Add(stage);
    public void AddPostExecutionStage(Type stage) => PostExecutionStages.Add(stage);

    public Type[] GetStages()
    {
        var all = new List<Type>();
        all.AddRange(PreExecutionStages);
        all.Add(typeof(THandler));
        all.AddRange(PostExecutionStages);
        return all.ToArray();
    }

}