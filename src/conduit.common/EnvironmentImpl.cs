using System.Collections;

namespace conduit.common;

/// <summary>
/// Provides a default implementation of <see cref="IEnvironment"/> that reads environment variables from the system.
/// </summary>
public class EnvironmentImpl : IEnvironment
{
    /// <summary>
    /// The name of the environment variable used to specify the environment name.
    /// </summary>
    public const string EnvironmentNameVariable = "ENVIRONMENT";
    
    /// <summary>
    /// The name of the environment variable used to specify the logging level.
    /// </summary>
    public const string LogLevelVariable = "LOG_LEVEL";

    /// <inheritdoc/>
    public LoggingLevel LogLevel
        => ParseEnum(LogLevelVariable, LoggingLevel.Info);

    /// <inheritdoc/>
    public EnvironmentName Environment 
        => ParseEnum(EnvironmentNameVariable, EnvironmentName.Unknown);

    /// <summary>
    /// Parses an environment variable as an enumeration value.
    /// </summary>
    /// <typeparam name="T">The enumeration type to parse to.</typeparam>
    /// <param name="variable">The name of the environment variable.</param>
    /// <param name="defaultValue">The default value to return if the variable is unset or not a valid name.</param>
    /// <returns>The parsed enumeration value, or the default value if parsing fails.</returns>
    private static T ParseEnum<T>(string variable, T defaultValue) where T : struct
        => Enum.TryParse(System.Environment.GetEnvironmentVariable(variable), ignoreCase: true, out T result)
            ? result
            : defaultValue;
    
    /// <inheritdoc/>
    public string? GetEnvironmentVariable(string variable)
        => System.Environment.GetEnvironmentVariable(variable);

    /// <inheritdoc/>
    public IDictionary<string, string> GetEnvironmentVariables()
    {
        var dict = new Dictionary<string, string>();
        foreach (DictionaryEntry entry in System.Environment.GetEnvironmentVariables())
            dict[(string)entry.Key] = (string?)entry.Value ?? string.Empty;

        return dict;
    }
}