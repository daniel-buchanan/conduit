namespace conduit.logging;

public interface IStdIo
{
    void WriteLine(string message);
    void WriteLine(string messageTemplate, params object[] propertyValues);
}

public interface IStdConsole
{
    IStdIo Out { get; }
    IStdIo Error { get; }
}

public class StdIo(bool isStdErr) : IStdIo
{
    public virtual void WriteLine(string message)
    {
        if(isStdErr) Console.Error.WriteLine(message);
        else Console.Out.WriteLine(message);
    }

    public virtual void WriteLine(string messageTemplate, params object[] propertyValues)
    {
        if(isStdErr) Console.Error.WriteLine(messageTemplate, propertyValues);
        else Console.Out.WriteLine(messageTemplate, propertyValues);
    }
}

public class StdConsole : IStdConsole
{
    public StdConsole()
    {
        Out = new StdIo(isStdErr: false);
        Error = new StdIo(isStdErr: true);
    }

    public StdConsole(IStdIo @out, IStdIo error)
    {
        Out = @out;
        Error = error;
    }
    
    public IStdIo Out { get; init; }
    public IStdIo Error { get; init; }
}