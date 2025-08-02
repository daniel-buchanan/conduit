using conduit.logging;

namespace conduit.Pipes.Stages;

public class DebugPostExecutionStage<T>(ILog logger) : PipeStage<T>(logger) where T : class
{
    protected override async Task<T> ExecuteInternalAsync(Guid instanceId, IRequest<T> request, Func<IRequest<T>, CancellationToken, Task<T>> next, CancellationToken cancellationToken)
    {
        var result = await next(request, cancellationToken);
        Logger.Debug("[POST] ICall<{0}>.ExecuteAsync", typeof(T).Name);
        return result;
    }
}