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
    /// <param name="value">The name of the environment variable.</param>
    /// <param name="defaultValue">The default value to return if parsing fails.</param>
    /// <returns>The parsed enumeration value, or the default value if parsing fails.</returns>
    private static T ParseEnum<T>(string value, T defaultValue) where T : struct
    {
        var parsed = Enum.TryParse(value, out T result);
        if(!parsed) return defaultValue;
        return result;
    }
    
    /// <inheritdoc/>
    public string? GetEnvironmentVariable(string variable)
        => System.Environment.GetEnvironmentVariable(variable);

    /// <inheritdoc/>
    public IDictionary<string, string> GetEnvironmentVariables()
    {
        var dict = new Dictionary<string, string>();
        foreach (var kp in System.Environment.GetEnvironmentVariables())
        {
            var t =  kp is KeyValuePair<string, string> pair ? pair : default;
            dict.Add(t.Key, t.Value);
        }

        return dict;
    }
}