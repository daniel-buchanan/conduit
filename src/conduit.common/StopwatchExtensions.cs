using System.Diagnostics;

namespace conduit.common;

public static class StopwatchExtensions
{
    public static void Restart(this Stopwatch stopwatch)
    {
        stopwatch.Reset();
        stopwatch.Start();
    }
}