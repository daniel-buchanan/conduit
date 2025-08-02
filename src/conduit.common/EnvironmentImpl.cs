namespace conduit.common;

public class EnvironmentImpl : IEnvironment
{
    public const string EnvironmentNameVariable = "ENVIRONMENT";
    public const string LogLevelVariable = "LOG_LEVEL";

    public LoggingLevel LogLevel
        => ParseEnum(LogLevelVariable, LoggingLevel.Info);

    public EnvironmentName Environment 
        => ParseEnum(EnvironmentNameVariable, EnvironmentName.Unknown);

    private static T ParseEnum<T>(string value, T defaultValue) where T : struct
    {
        var parsed = Enum.TryParse(value, out T result);
        if(!parsed) return defaultValue;
        return result;
    }
    
    public string? GetEnvironmentVariable(string variable)
        => System.Environment.GetEnvironmentVariable(variable);

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