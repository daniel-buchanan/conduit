namespace conduit.common;

/// <summary>
/// Provides extension methods for synchronously waiting on asynchronous operations.
/// </summary>
public static class AsyncExtensions
{
    /// <summary>
    /// Synchronously waits for an asynchronous task to complete. A faulted task's exception is rethrown as
    /// itself (via <see cref="TaskAwaiter.GetResult"/>), not wrapped in <see cref="AggregateException"/> the
    /// way <see cref="Task.Wait(CancellationToken)"/>/<see cref="Task{TResult}.Result"/> would.
    /// </summary>
    /// <param name="task">The task to wait for.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    public static void Await(this Task task, CancellationToken cancellationToken = default)
    {
        task.WaitAsync(cancellationToken).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Synchronously waits for an asynchronous task to complete and returns the result. A faulted task's
    /// exception is rethrown as itself (via <see cref="TaskAwaiter{TResult}.GetResult"/>), not wrapped in
    /// <see cref="AggregateException"/> the way <see cref="Task.Wait(CancellationToken)"/>/<see cref="Task{TResult}.Result"/> would.
    /// </summary>
    /// <typeparam name="T">The type of the result returned by the task.</typeparam>
    /// <param name="task">The task to wait for.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The result of the task.</returns>
    public static T Await<T>(this Task<T> task, CancellationToken cancellationToken = default)
        => task.WaitAsync(cancellationToken).GetAwaiter().GetResult();
}