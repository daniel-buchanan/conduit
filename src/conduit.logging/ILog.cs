namespace conduit.logging;

public interface ILog
{
    ILog Debug(string message);
    ILog Debug(string messageTemplate, params object[] propertyValues);
    ILog Debug(string message, Exception ex);
    
    ILog Verbose(string message);
    ILog Verbose(string messageTemplate, params object[] propertyValues);
    ILog Verbose(string message, Exception ex);
    
    ILog Info(string message);
    ILog Info(string messageTemplate, params object[] propertyValues);
    ILog Info(string message, Exception ex);
    
    ILog Warn(string message);
    ILog Warn(string messageTemplate, params object[] propertyValues);
    ILog Warn(string message, Exception ex);
    
    ILog Error(string message);
    ILog Error(string messageTemplate, params object[] propertyValues);
    ILog Error(string message, Exception ex);
    ILog Error(Exception ex);
    ILog Error(Exception ex, string messageTemplate, params object[] propertyValues);
}