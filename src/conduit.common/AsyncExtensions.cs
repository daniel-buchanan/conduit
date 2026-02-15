namespace conduit.common;

/// <summary>
/// Provides extension methods for synchronously waiting on asynchronous operations.
/// </summary>
public static class AsyncExtensions
{
    /// <summary>
    /// Synchronously waits for an asynchronous task to complete.
    /// </summary>
    /// <param name="task">The task to wait for.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    public static void Await(this Task task, CancellationToken cancellationToken = default)
    {
        task.Wait(cancellationToken);
    }

    /// <summary>
    /// Synchronously waits for an asynchronous task to complete and returns the result.
    /// </summary>
    /// <typeparam name="T">The type of the result returned by the task.</typeparam>
    /// <param name="task">The task to wait for.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The result of the task.</returns>
    public static T Await<T>(this Task<T> task, CancellationToken cancellationToken = default)
    {
        task.Wait(cancellationToken);
        return task.Result;
    }
}