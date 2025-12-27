using System.Diagnostics;
using conduit.common;
using conduit.Exceptions;
using conduit.logging;
using conduit.Pipes.Stages;

namespace conduit.Pipes;

/// <inheritdoc/>
public abstract class Pipe<TRequest, TResponse>(
    ILog logger,
    IServiceProvider provider) : IPipe<TRequest, TResponse> 
    where TRequest : class, IRequest<TResponse> 
    where TResponse : class
{
    /// <inheritdoc/>
    public abstract Task<TResponse?> PushAsync(TRequest request, CancellationToken cancellationToken = default);

    /// <inheritdoc/>
    public abstract Task<DebugResult<TResponse?>> PushWithDebugAsync(TRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute an individual pipeline stage, with or without metrics.
    /// </summary>
    /// <param name="index">The index of the stage.</param>
    /// <param name="instanceId">The ID of the pipeline run.</param>
    /// <param name="stageType">The stage to be executed.</param>
    /// <param name="stageTimer">The timer being used for getting stage metrics.</param>
    /// <param name="request">The request being processed.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <param name="withMetrics">Whether to provide metrics on the stage.</param>
    /// <returns>A record containing the Respons and optionally the Metrics for the Stage.</returns>
    /// <exception cref="StageNotFoundException">This exception will be thrown in the stage cannot be found.</exception>
    protected async Task<(TResponse? Response, StageMetric? Metric)> ExecuteStage(
        int index,
        Guid instanceId, 
        Type stageType,
        Stopwatch? stageTimer,
        TRequest request,
        CancellationToken cancellationToken,
        bool withMetrics)
    {
        stageTimer?.Restart();
        StageMetric? metric = null;
        TResponse? response = null;
        
        var stage = (IPipeStage<TRequest, TResponse>?)provider.GetService(stageType);
        var stageName = stageType.GetGenericName();
        if (stage == null)
            throw new StageNotFoundException($"Could not resolve stage of type {stageName}");
            
        logger.Debug($"[{instanceId}] {stageType.GetGenericName()} :: Executing stage {stageName}");
        try
        {
            var stageResponse = await stage.ExecuteAsync(instanceId, request, cancellationToken);

            if (!stageResponse.IsSuccessful) HandleUnsuccessfulResult(request, stageResponse);
            
            stageTimer?.Stop();
            if(withMetrics)
                metric = new StageMetric(index, stageType.GetGenericName(), stageTimer?.ElapsedMilliseconds ?? -1);
        }
        catch (Exception e)
        {
            stageTimer?.Stop();
            logger.Error($"[{instanceId}] {stageType.GetGenericName()} :: Error while executing stage {stageName}", e);
            throw;
        }
        
        return (response, metric);
    }

    private void HandleUnsuccessfulResult(TRequest request, StageResult<TRequest, TResponse> stageResponse)
    {
        if (stageResponse.ValidationErrors.Length != 0)
        {
            throw new ValidationFailedException(ValidationResult.WithFailure(stageResponse.Result,
                stageResponse.ValidationErrors));
        }
        
        var message = $"Stage {stageResponse.StageType.GetGenericName()} Failed with Exception {stageResponse.Exception!.Message}";
        throw new StageFailedException(message, stageResponse.Exception);
    }
}