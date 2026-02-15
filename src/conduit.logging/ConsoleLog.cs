using conduit.common;

namespace conduit.logging;

/// <summary>
/// Provides a logging implementation that writes messages to the console output.
/// </summary>
/// <param name="environment">The environment configuration for determining log levels.</param>
/// <param name="console">The standard console I/O abstraction to use for output.</param>
public class ConsoleLog(
    IEnvironment environment, 
    IStdConsole console) : Log(environment)
{
    private const string LogFormat = "[{0}] {1:yyyy-MM-ddThh:mm:ss} {2}";
    
    /// <inheritdoc/>
    protected override ILog WriteMessageInternal(string level, string message)
    {
        var timestamp = DateTimeOffset.UtcNow;
        console.Out.WriteLine(LogFormat, level, timestamp, message);
        return this;
    }
}