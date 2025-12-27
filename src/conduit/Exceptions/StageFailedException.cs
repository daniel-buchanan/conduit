namespace conduit.Exceptions;

public class StageFailedException : Exception
{
    public StageFailedException() : base("Stage Failed during Execution") { }

    public StageFailedException(string message) : base(message) { }
    
    public StageFailedException(string message, Exception inner) : base(message, inner) { }
}