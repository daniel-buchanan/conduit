using System.Diagnostics;
using conduit.common;

namespace conduit.logging;

/// <summary>
/// Provides an abstract base class for logging implementations in the Conduit system.
/// This class implements the <see cref="ILog"/> interface and provides common logging logic.
/// </summary>
/// <param name="environment">The environment configuration for determining log levels.</param>
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
    
    /// <summary>
    /// Gets the environment configuration used by this logger.
    /// </summary>
    protected readonly IEnvironment Environment = environment;

    /// <inheritdoc/>
    public ILog Debug(string message)
        => Debug(message, propertyValues: []);

    /// <inheritdoc/>
    public ILog Debug(string messageTemplate, params object[] propertyValues) 
        => WriteMessage(LoggingLevel.Debug, messageTemplate, propertyValues);

    /// <inheritdoc/>
    public ILog Debug(string message, Exception ex)
    {
        Debug(message);
        Debug(ErrorMessageTemplate, ex.Message);
        return ex.StackTrace != null 
            ? Debug("{0}", ex.StackTrace) 
            : this;
    }

    /// <inheritdoc/>
    public ILog Verbose(string message)
        => Verbose(message, propertyValues: []);

    /// <inheritdoc/>
    public ILog Verbose(string messageTemplate, params object[] propertyValues)
        => WriteMessage(LoggingLevel.Verbose, messageTemplate, propertyValues);

    /// <inheritdoc/>
    public ILog Verbose(string message, Exception ex)
        => Verbose(ErrorMessageTemplate, ex.Message);

    /// <inheritdoc/>
    public ILog Info(string message)
        => Info(message, propertyValues: []);

    /// <inheritdoc/>
    public ILog Info(string messageTemplate, params object[] propertyValues)
        => WriteMessage(LoggingLevel.Info, messageTemplate, propertyValues);

    /// <inheritdoc/>
    public ILog Info(string message, Exception ex)
        => Info(ErrorMessageTemplate, ex.Message);

    /// <inheritdoc/>
    public ILog Warn(string message)
        => Warn(message, propertyValues: []);

    /// <inheritdoc/>
    public ILog Warn(string messageTemplate, params object[] propertyValues)
        => WriteMessage(LoggingLevel.Warning, messageTemplate, propertyValues);

    /// <inheritdoc/>
    public ILog Warn(string message, Exception ex)
        => Warn(ErrorMessageTemplate, ex.Message);

    /// <inheritdoc/>
    public ILog Error(string message)
        => Error(message, propertyValues: []);

    /// <inheritdoc/>
    public ILog Error(string messageTemplate, params object[] propertyValues)
        => WriteMessage(LoggingLevel.Error, messageTemplate, propertyValues);

    /// <inheritdoc/>
    public ILog Error(string message, Exception ex)
    {
        Error(message);
        return Error(ErrorMessageTemplate, ex.Message);
    }

    /// <inheritdoc/>
    public ILog Error(Exception ex)
        => Error(ErrorMessageTemplate, ex.Message);

    /// <inheritdoc/>
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

    /// <summary>
    /// When overridden in a derived class, writes the formatted message to the logging output.
    /// </summary>
    /// <param name="level">The formatted logging level string.</param>
    /// <param name="message">The formatted message to write.</param>
    /// <returns>The current logger instance for method chaining.</returns>
    protected abstract ILog WriteMessageInternal(string level, string message);
}