namespace conduit;

public class Conduit : IConduit
{
    public Task<T> PushAsync<T>(IRequest<T> request, CancellationToken cancellationToken = default) where T : class, IResponse
    {
        throw new NotImplementedException();
    }
}