namespace conduit.validation;

public class ValidatorNotFoundException : Exception
{
    public ValidatorNotFoundException(string msg) : base(msg) { }
    public ValidatorNotFoundException(string msg, Exception innerException) : base(msg, innerException) { }
    
}