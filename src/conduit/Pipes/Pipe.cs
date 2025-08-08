using System.Diagnostics;
using conduit.logging;

namespace conduit.Pipes;

public abstract class Pipe<TResponse> : IPipe<TResponse>
    where TResponse : class
{
    private readonly IPipeStage<TResponse>[] _pipeStages;
    private readonly ILog _logger;

    protected Pipe(ILog logger, IPipeInput<TResponse> input)
    {
        _logger = logger;
        _pipeStages = [.. input.Stages];
    }

    public async Task<TResponse?> SendAsync(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        var startTime = Stopwatch.GetTimestamp();
        var instanceId = Guid.NewGuid();
        var context = new PipeContext<TResponse>(_pipeStages.Length, request);
        for (var index = 0; index < _pipeStages.Length; index++)
        {
            var stageStartTime = Stopwatch.GetTimestamp();
            TResponse? response = null;
            Exception? exception = null;
            bool isSuccess = false;
            try
            {
                response = await _pipeStages[index].ExecuteAsync(instanceId, request);
                isSuccess = true;
            }
            catch (Exception ex)
            {
                exception = ex;
            }
            var stageEndTime = Stopwatch.GetTimestamp();
            var duration = stageEndTime - stageStartTime;
            context.SetStageMetric(index, new StageMetric(index, _pipeStages[index].GetType().Name, duration, isSuccess, exception));
            if (response != null)
                context.SetResponse(response);
        }
        var endTime = Stopwatch.GetTimestamp();
        var totalDuration = endTime - startTime;
        _logger.Debug($"Pipe execution completed in {totalDuration} ticks for instance {instanceId} with response: {context.Response?.ToString() ?? "null"}");
        return context.Response;
    }
}

public interface IPipeInput<TResponse>
    where TResponse : class
{
    IEnumerable<IPipeStage<TResponse>> Stages { get; }
}