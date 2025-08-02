namespace conduit.Pipes;

public class PipeContext<TResponse>(int countOfStages, IRequest<TResponse> request) : IPipeContext<TResponse> 
    where TResponse : class
{
    public IRequest<TResponse> Request { get; } = request;
    public TResponse? Response { get; private set; }
    public int CountOfStages { get; } = countOfStages;
    public StageMetric[] Metrics { get; } = new StageMetric[countOfStages];
    
    public void SetResponse(TResponse? response)
        => Response = response;

    public void SetStageMetric(int index, StageMetric metric) 
        => Metrics[index] = metric;
}