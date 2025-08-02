namespace conduit;

public interface IConduit
{
    Task<T> PushAsync<T>(IRequest<T> request, CancellationToken cancellationToken = default)
        where T : class, IResponse;
}