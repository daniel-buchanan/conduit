using conduit.logging;

namespace conduit.tests.Logging;

public class MockStdIo(bool isStdErr) : StdIo(isStdErr)
{
    public event Action<string>? OnWriteLine;

    public override void WriteLine(string message)
    {
        OnWriteLine?.Invoke(message);
        base.WriteLine(message);
    }

    public override void WriteLine(string messageTemplate, params object[] propertyValues)
    {
        OnWriteLine?.Invoke(string.Format(messageTemplate, propertyValues));
        base.WriteLine(messageTemplate, propertyValues);
    }
}