namespace conduit.logging;

/// <summary>
/// Defines the contract for logging operations in the Conduit system.
/// </summary>
public interface ILog
{
    /// <summary>
    /// Logs a debug-level message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <returns>The current logger instance for method chaining.</returns>
    ILog Debug(string message);
    
    /// <summary>
    /// Logs a debug-level message with formatted parameters.
    /// </summary>
    /// <param name="messageTemplate">The message template with format placeholders.</param>
    /// <param name="propertyValues">The values to substitute into the message template.</param>
    /// <returns>The current logger instance for method chaining.</returns>
    ILog Debug(string messageTemplate, params object[] propertyValues);
    
    /// <summary>
    /// Logs a debug-level message with an associated exception.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="ex">The exception associated with this log entry.</param>
    /// <returns>The current logger instance for method chaining.</returns>
    ILog Debug(string message, Exception ex);
    
    /// <summary>
    /// Logs a verbose-level message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <returns>The current logger instance for method chaining.</returns>
    ILog Verbose(string message);
    
    /// <summary>
    /// Logs a verbose-level message with formatted parameters.
    /// </summary>
    /// <param name="messageTemplate">The message template with format placeholders.</param>
    /// <param name="propertyValues">The values to substitute into the message template.</param>
    /// <returns>The current logger instance for method chaining.</returns>
    ILog Verbose(string messageTemplate, params object[] propertyValues);
    
    /// <summary>
    /// Logs a verbose-level message with an associated exception.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="ex">The exception associated with this log entry.</param>
    /// <returns>The current logger instance for method chaining.</returns>
    ILog Verbose(string message, Exception ex);
    
    /// <summary>
    /// Logs an info-level message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <returns>The current logger instance for method chaining.</returns>
    ILog Info(string message);
    
    /// <summary>
    /// Logs an info-level message with formatted parameters.
    /// </summary>
    /// <param name="messageTemplate">The message template with format placeholders.</param>
    /// <param name="propertyValues">The values to substitute into the message template.</param>
    /// <returns>The current logger instance for method chaining.</returns>
    ILog Info(string messageTemplate, params object[] propertyValues);
    
    /// <summary>
    /// Logs an info-level message with an associated exception.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="ex">The exception associated with this log entry.</param>
    /// <returns>The current logger instance for method chaining.</returns>
    ILog Info(string message, Exception ex);
    
    /// <summary>
    /// Logs a warning-level message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <returns>The current logger instance for method chaining.</returns>
    ILog Warn(string message);
    
    /// <summary>
    /// Logs a warning-level message with formatted parameters.
    /// </summary>
    /// <param name="messageTemplate">The message template with format placeholders.</param>
    /// <param name="propertyValues">The values to substitute into the message template.</param>
    /// <returns>The current logger instance for method chaining.</returns>
    ILog Warn(string messageTemplate, params object[] propertyValues);
    
    /// <summary>
    /// Logs a warning-level message with an associated exception.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="ex">The exception associated with this log entry.</param>
    /// <returns>The current logger instance for method chaining.</returns>
    ILog Warn(string message, Exception ex);
    
    /// <summary>
    /// Logs an error-level message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <returns>The current logger instance for method chaining.</returns>
    ILog Error(string message);
    
    /// <summary>
    /// Logs an error-level message with formatted parameters.
    /// </summary>
    /// <param name="messageTemplate">The message template with format placeholders.</param>
    /// <param name="propertyValues">The values to substitute into the message template.</param>
    /// <returns>The current logger instance for method chaining.</returns>
    ILog Error(string messageTemplate, params object[] propertyValues);
    
    /// <summary>
    /// Logs an error-level message with an associated exception.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="ex">The exception associated with this log entry.</param>
    /// <returns>The current logger instance for method chaining.</returns>
    ILog Error(string message, Exception ex);
    
    /// <summary>
    /// Logs an exception as an error-level message.
    /// </summary>
    /// <param name="ex">The exception to log.</param>
    /// <returns>The current logger instance for method chaining.</returns>
    ILog Error(Exception ex);
    
    /// <summary>
    /// Logs an exception as an error-level message with formatted context.
    /// </summary>
    /// <param name="ex">The exception to log.</param>
    /// <param name="messageTemplate">The message template with format placeholders.</param>
    /// <param name="propertyValues">The values to substitute into the message template.</param>
    /// <returns>The current logger instance for method chaining.</returns>
    ILog Error(Exception ex, string messageTemplate, params object[] propertyValues);
}