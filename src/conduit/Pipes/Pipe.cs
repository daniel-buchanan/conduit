namespace conduit.Pipes;

public abstract class Pipe<TResponse> : IPipe<TResponse> 
    where TResponse : class
{
    public Task<TResponse> SendAsync(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        var instanceId = Guid.NewGuid();
        var context = new PipeContext();
        throw new NotImplementedException();
    }
}