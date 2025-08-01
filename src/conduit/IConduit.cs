namespace conduit;

public interface IConduit
{
    Task<T> EnquireAsync<T>(IInquiry<T> inquiry, CancellationToken cancellationToken = default)
        where T : class, IReply;
}