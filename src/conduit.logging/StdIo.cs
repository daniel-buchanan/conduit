namespace conduit.logging;

/// <summary>
/// Defines the contract for writing to standard I/O streams.
/// </summary>
public interface IStdIo
{
    /// <summary>
    /// Writes a line to the output stream.
    /// </summary>
    /// <param name="message">The message to write.</param>
    void WriteLine(string message);
    
    /// <summary>
    /// Writes a formatted line to the output stream.
    /// </summary>
    /// <param name="messageTemplate">The message template with format placeholders.</param>
    /// <param name="propertyValues">The values to substitute into the message template.</param>
    void WriteLine(string messageTemplate, params object[] propertyValues);
}

/// <summary>
/// Defines the contract for accessing standard output and error streams.
/// </summary>
public interface IStdConsole
{
    /// <summary>
    /// Gets the standard output stream abstraction.
    /// </summary>
    IStdIo Out { get; }
    
    /// <summary>
    /// Gets the standard error stream abstraction.
    /// </summary>
    IStdIo Error { get; }
}

/// <summary>
/// Provides a wrapper around standard output or error streams.
/// </summary>
/// <param name="isStdErr">If true, writes to standard error; otherwise writes to standard output.</param>
public class StdIo(bool isStdErr) : IStdIo
{
    /// <inheritdoc/>
    public virtual void WriteLine(string message)
    {
        if(isStdErr) Console.Error.WriteLine(message);
        else Console.Out.WriteLine(message);
    }

    /// <inheritdoc/>
    public virtual void WriteLine(string messageTemplate, params object[] propertyValues)
    {
        if(isStdErr) Console.Error.WriteLine(messageTemplate, propertyValues);
        else Console.Out.WriteLine(messageTemplate, propertyValues);
    }
}

/// <summary>
/// Provides a default implementation of <see cref="IStdConsole"/> that wraps the system console streams.
/// </summary>
public class StdConsole : IStdConsole
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StdConsole"/> class with default console streams.
    /// </summary>
    public StdConsole()
    {
        Out = new StdIo(isStdErr: false);
        Error = new StdIo(isStdErr: true);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StdConsole"/> class with custom output and error streams.
    /// </summary>
    /// <param name="out">The standard output stream abstraction.</param>
    /// <param name="error">The standard error stream abstraction.</param>
    public StdConsole(IStdIo @out, IStdIo error)
    {
        Out = @out;
        Error = error;
    }
    
    /// <inheritdoc/>
    public IStdIo Out { get; init; }
    
    /// <inheritdoc/>
    public IStdIo Error { get; init; }
}