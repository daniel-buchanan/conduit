namespace conduit.Exceptions;

public class PipeAlreadyRegisteredException : Exception
{
    public PipeAlreadyRegisteredException() : base("Pipe already Registered") { }
    public PipeAlreadyRegisteredException(string message) : base(message) { }
    public PipeAlreadyRegisteredException(string message, Exception inner) : base(message, inner) { }
}