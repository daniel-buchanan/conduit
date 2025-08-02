using conduit.logging;

namespace conduit.Pipes.Stages;

public class DebugPreExecutionStage<T>(ILog logger) : PipeStage<T>(logger) where T : class
{
    protected override async Task<T> ExecuteInternalAsync(Guid instanceId, IRequest<T> request, Func<IRequest<T>, CancellationToken, Task<T>> next, CancellationToken cancellationToken)
    {
        Logger.Debug("[PRE] ICall<{0}>.ExecuteAsync", typeof(T).Name);
        return await next(request, cancellationToken); 
    }
}