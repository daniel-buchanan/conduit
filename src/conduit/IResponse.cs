namespace conduit;

public interface IResponse;

public interface IResponse<T> : IResponse 
    where T: class
{
    bool Success { get; }
}