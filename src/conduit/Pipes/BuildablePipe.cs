using System.Diagnostics;
using conduit.logging;

namespace conduit.Pipes;

/// <summary>
/// Provides a class which is the result of using a builder to create a pipe rather than creating it from the ground up.
/// </summary>
/// <typeparam name="TRequest">The type of the Request.</typeparam>
/// <typeparam name="TResponse">The type of the Response</typeparam>
public class BuildablePipe<TRequest, TResponse>(ILog logger, IServiceProvider serviceProvider, Type[] stages)
    : Pipe<TRequest, TResponse>(logger, serviceProvider)
    where TResponse : class
    where TRequest : class, IRequest<TResponse>
{
    /// <inheritdoc/>
    public override async Task<TResponse?> PushAsync(TRequest request, CancellationToken cancellationToken = default)
    {
        var result = await PushInternalAsync(request, withMetrics: false, cancellationToken);
        return result.Response;
    }

    /// <inheritdoc/>
    public override async Task<DebugResult<TResponse?>> PushWithDebugAsync(TRequest request, CancellationToken cancellationToken = default)
    {
        var result = await PushInternalAsync(request, withMetrics: true, cancellationToken);
        return new DebugResult<TResponse?>(result.Response, result.OverallDurationMs!.Value, result.Metrics!);
    }

    private async Task<(TResponse? Response, long? OverallDurationMs, StageMetric[]? Metrics)> PushInternalAsync(
        TRequest request,
        bool withMetrics,
        CancellationToken cancellationToken = default)
    {
        TResponse? response = null;
        StageMetric[]? metrics = null;
        Stopwatch? overallTimer = null;
        Stopwatch? stageTimer = null;

        if (withMetrics)
        {
            metrics = new StageMetric[stages.Length];
            overallTimer = Stopwatch.StartNew();
            stageTimer = new Stopwatch();
        }
        
        var instanceId = Guid.NewGuid();
        for (var i = 0; i < stages.Length; i++)
        {
            var result = await ExecuteStage(i, instanceId, stages[i], stageTimer, request, cancellationToken, withMetrics);
            if (metrics != null) metrics[i] = result.Metric!;
            if(response is null) response = result.Response;
        }
        
        overallTimer?.Stop();
        return (response, overallTimer?.ElapsedMilliseconds, metrics);
    }
}