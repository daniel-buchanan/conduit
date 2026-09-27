using conduit.logging;

namespace conduit.benchmarks;

/// <summary>
/// Discards everything. Keeps console I/O out of the measured path.
/// </summary>
public class NullLog : ILog
{
    public ILog Debug(string message) => this;
    public ILog Debug(string messageTemplate, params object[] propertyValues) => this;
    public ILog Debug(string message, Exception ex) => this;

    public ILog Verbose(string message) => this;
    public ILog Verbose(string messageTemplate, params object[] propertyValues) => this;
    public ILog Verbose(string message, Exception ex) => this;

    public ILog Info(string message) => this;
    public ILog Info(string messageTemplate, params object[] propertyValues) => this;
    public ILog Info(string message, Exception ex) => this;

    public ILog Warn(string message) => this;
    public ILog Warn(string messageTemplate, params object[] propertyValues) => this;
    public ILog Warn(string message, Exception ex) => this;

    public ILog Error(string message) => this;
    public ILog Error(string messageTemplate, params object[] propertyValues) => this;
    public ILog Error(string message, Exception ex) => this;
    public ILog Error(Exception ex) => this;
    public ILog Error(Exception ex, string messageTemplate, params object[] propertyValues) => this;
}
