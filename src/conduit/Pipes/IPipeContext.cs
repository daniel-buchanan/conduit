namespace conduit.Pipes;

public interface IPipeContext<T>
    where T: class
{
    IRequest<T> Request { get; }
    T? Response { get; }
    int CountOfStages { get; }
    StageMetric[] Metrics { get; }
    void SetResponse(T? response);
    void SetStageMetric(int index, StageMetric metric);
}