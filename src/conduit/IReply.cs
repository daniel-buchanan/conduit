namespace conduit;

public interface IReply;

public interface IReply<T> : IReply 
    where T: class
{
    bool Success { get; }
}