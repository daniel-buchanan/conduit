using System.Collections;

namespace conduit.common;

public interface IEnvironment
{
    LoggingLevel LogLevel { get; }
    EnvironmentName Environment { get; }
    string? GetEnvironmentVariable(string variable);
    IDictionary<string, string> GetEnvironmentVariables();
}