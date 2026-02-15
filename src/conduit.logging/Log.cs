using System.Diagnostics;
using conduit.common;

namespace conduit.logging;

public abstract class Log(IEnvironment environment) : ILog
{
    private static class Levels
    {
        public const string Debug = "DEBUG";
        public const string Verbose = "VERB";
        public const string Information = "INFO";
        public const string Warning = "WARN";
        public const string Error = "ERR";
    }
    
    private const string ErrorMessageTemplate = "Error occurred! {0}";
    
    protected readonly IEnvironment Environment = environment;

    public ILog Debug(string message)
        => Debug(message, propertyValues: []);

    public ILog Debug(string messageTemplate, params object[] propertyValues) 
        => WriteMessage(LoggingLevel.Debug, messageTemplate, propertyValues);

    public ILog Debug(string message, Exception ex)
    {
        Debug(message);
        Debug(ErrorMessageTemplate, ex.Message);
        return ex.StackTrace != null 
            ? Debug("{0}", ex.StackTrace) 
            : this;
    }

    public ILog Verbose(string message)
        => Verbose(message, propertyValues: []);

    public ILog Verbose(string messageTemplate, params object[] propertyValues)
        => WriteMessage(LoggingLevel.Verbose, messageTemplate, propertyValues);

    public ILog Verbose(string message, Exception ex)
        => Verbose(ErrorMessageTemplate, ex.Message);

    public ILog Info(string message)
        => Info(message, propertyValues: []);

    public ILog Info(string messageTemplate, params object[] propertyValues)
        => WriteMessage(LoggingLevel.Info, messageTemplate, propertyValues);

    public ILog Info(string message, Exception ex)
        => Info(ErrorMessageTemplate, ex.Message);

    public ILog Warn(string message)
        => Warn(message, propertyValues: []);

    public ILog Warn(string messageTemplate, params object[] propertyValues)
        => WriteMessage(LoggingLevel.Warning, messageTemplate, propertyValues);

    public ILog Warn(string message, Exception ex)
        => Warn(ErrorMessageTemplate, ex.Message);

    public ILog Error(string message)
        => Error(message, propertyValues: []);

    public ILog Error(string messageTemplate, params object[] propertyValues)
        => WriteMessage(LoggingLevel.Error, messageTemplate, propertyValues);

    public ILog Error(string message, Exception ex)
    {
        Error(message);
        return Error(ErrorMessageTemplate, ex.Message);
    }

    public ILog Error(Exception ex)
        => Error(ErrorMessageTemplate, ex.Message);

    public ILog Error(Exception ex, string messageTemplate, params object[] propertyValues)
    {
        Error(messageTemplate, propertyValues);
        return Error(ex);
    }

    private bool AllowedToLog(LoggingLevel level)
        => level >= Environment.LogLevel;

    private ILog WriteMessage(LoggingLevel level, string messageTemplate, params object[] propertyValues)
    {
        var logLevelStr = level switch
        {
            LoggingLevel.Debug => Levels.Debug,
            LoggingLevel.Verbose => Levels.Verbose,
            LoggingLevel.Info => Levels.Information,
            LoggingLevel.Warning => Levels.Warning,
            LoggingLevel.Error => Levels.Error,
            _ => Levels.Information
        };

        if (logLevelStr.Length < 5)
        {
            var remainingSpace = 5 - logLevelStr.Length;
            logLevelStr = logLevelStr.PadRight(remainingSpace);
        }
        
        var allowedToLog = AllowedToLog(level);
        return !allowedToLog
            ? this
            : WriteMessageInternal(logLevelStr, string.Format(messageTemplate, propertyValues));
    }

    protected abstract ILog WriteMessageInternal(string level, string message);
}