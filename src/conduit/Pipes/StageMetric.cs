namespace conduit.Pipes;

public record StageMetric(int Index, string Name, long Duration, bool Success, Exception? Exception = null)
{
    public int Index { get; } = Index;
    public string Name { get; } = Name;
    public long Duration { get; } = Duration;
    public bool Success { get; } = Success;
    public Exception? Exception { get; } = Exception;
}