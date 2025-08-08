namespace conduit;

public interface IRequest<TResult> where TResult : class;

public interface IRequestHandler<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : class
{
    Task<TResponse?> HandleAsync(TRequest request, CancellationToken cancellationToken = default);
}