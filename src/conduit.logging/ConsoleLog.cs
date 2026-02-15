using conduit.common;

namespace conduit.logging;

public class ConsoleLog(
    IEnvironment environment, 
    IStdConsole console) : Log(environment)
{
    private const string LogFormat = "[{0}] {1:yyyy-MM-ddThh:mm:ss} {2}";
    
    protected override ILog WriteMessageInternal(string level, string message)
    {
        var timestamp = DateTimeOffset.UtcNow;
        console.Out.WriteLine(LogFormat, level, timestamp, message);
        return this;
    }
}