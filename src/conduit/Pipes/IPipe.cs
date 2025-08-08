namespace conduit.Pipes;

public interface IPipe;

public interface IPipe<TResponse> : IPipe
    where TResponse : class
{
    Task<TResponse?> SendAsync(IRequest<TResponse> request, CancellationToken cancellationToken = default);
}