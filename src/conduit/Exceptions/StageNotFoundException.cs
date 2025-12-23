namespace conduit.Exceptions;

public class StageNotFoundException : Exception
{
    public StageNotFoundException() : base("Pipe not found") { }
    public StageNotFoundException(string message) : base(message) { }
    public StageNotFoundException(string message, Exception inner) : base(message, inner) { }
}