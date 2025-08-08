namespace conduit.Pipes;

public interface IPipeStage<T> : IRequest<T> where T: class
{
    Task<T?> ExecuteAsync(Guid instanceId, IRequest<T> request, CancellationToken cancellationToken = default);
}