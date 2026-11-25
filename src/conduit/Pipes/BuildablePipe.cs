using System.Diagnostics;
using conduit.common;
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
        return new DebugResult<TResponse?>(result.Response, result.OverallDurationMs!.Value, result.Metrics!, result.ShortCircuited);
    }

    private async Task<(TResponse? Response, long? OverallDurationMs, StageMetric[]? Metrics, bool ShortCircuited)> PushInternalAsync(
        TRequest request,
        bool withMetrics,
        CancellationToken cancellationToken = default)
    {
        TResponse? response = null;
        StageMetric[]? metrics = null;
        Stopwatch? overallTimer = null;
        Stopwatch? stageTimer = null;
        var shortCircuited = false;

        if (withMetrics)
        {
            metrics = new StageMetric[stages.Length];
            overallTimer = Stopwatch.StartNew();
            stageTimer = new Stopwatch();
        }

        var instanceId = Guid.NewGuid();
        var stagesRun = stages.Length;
        for (var i = 0; i < stages.Length; i++)
        {
            var result = await ExecuteStage(i, instanceId, stages[i], stageTimer, request, cancellationToken, withMetrics);
            metrics?[i] = result.Metric!;

            if (response is not null && result.Response is not null && !Equals(response, result.Response))
                Logger.Verbose($"[{instanceId}] {stages[i].GetGenericName()} :: Stage response overrode previous stage's response.");

            response = result.Response ?? response;

            if (result.ShortCircuited)
            {
                shortCircuited = true;
                stagesRun = i + 1;
                break;
            }
        }

        overallTimer?.Stop();
        if (metrics is not null && stagesRun < metrics.Length)
            Array.Resize(ref metrics, stagesRun);

        return (response, overallTimer?.ElapsedMilliseconds, metrics, shortCircuited);
    }
}