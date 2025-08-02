namespace conduit.Pipes;

public interface IPipeStage<T> : IRequest<T> where T: class
{
    Task<T> ExecuteAsync(Guid instanceId, IRequest<T> request, CancellationToken cancellationToken = default);
    Task<T> ExecuteAsync(
        Guid instanceId, 
        IRequest<T> request,
        Func<IRequest<T>, CancellationToken, Task<T>> next,
        CancellationToken cancellationToken = default);
}