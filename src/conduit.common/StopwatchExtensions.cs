using System.Diagnostics;

namespace conduit.common;

/// <summary>
/// Provides extension methods for working with the Stopwatch class.
/// </summary>
public static class StopwatchExtensions
{
    /// <summary>
    /// Resets and starts a stopwatch in a single operation.
    /// </summary>
    /// <param name="stopwatch">The stopwatch to restart.</param>
    public static void Restart(this Stopwatch stopwatch)
    {
        stopwatch.Reset();
        stopwatch.Start();
    }
}