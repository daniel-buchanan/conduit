namespace conduit.common;

public static class AsyncExtensions
{
    public static void Await(this Task task, CancellationToken cancellationToken = default)
    {
        task.Wait(cancellationToken);
    }

    public static T Await<T>(this Task<T> task, CancellationToken cancellationToken = default)
    {
        task.Wait(cancellationToken);
        return task.Result;
    }
}