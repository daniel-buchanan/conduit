namespace conduit.Exceptions;

public class PipeNotFoundException : Exception
{
    public PipeNotFoundException() : base("Pipe not found") { }
    public PipeNotFoundException(string message) : base(message) { }
    public PipeNotFoundException(string message, Exception inner) : base(message, inner) { }
}