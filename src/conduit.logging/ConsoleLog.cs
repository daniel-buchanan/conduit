using conduit.common;

namespace conduit.logging;

public class ConsoleLog(IEnvironment environment) : Log(environment)
{
    protected override ILog WriteMessageInternal(string level, string message)
    {
        var timestamp = DateTimeOffset.UtcNow;
        Console.WriteLine("[{0}] {1:yyyy-MM-ddThh:mm:ss} {2}", level, timestamp, message);
        return this;
    }
}