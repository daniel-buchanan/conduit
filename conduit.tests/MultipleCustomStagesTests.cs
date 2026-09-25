using conduit.logging;
using conduit.Pipes;
using conduit.Pipes.Stages;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace conduit.tests;

public class MultipleCustomStagesTests
{
    public class MultiStageRequest : IRequest<MultiStageResponse>;

    public class MultiStageResponse
    {
        public string Value { get; set; } = string.Empty;
    }

    public class FirstStage(ILog logger) : PipeStage<MultiStageRequest, MultiStageResponse>(logger)
    {
        protected override Task<StageResult<MultiStageRequest, MultiStageResponse>> ExecuteInternalAsync(
            Guid instanceId, MultiStageRequest request, CancellationToken cancellationToken)
            => Task.FromResult(StageResult.WithResult<MultiStageRequest, MultiStageResponse>(new MultiStageResponse { Value = "first" }, GetType()));
    }

    public class SecondStage(ILog logger) : PipeStage<MultiStageRequest, MultiStageResponse>(logger)
    {
        protected override Task<StageResult<MultiStageRequest, MultiStageResponse>> ExecuteInternalAsync(
            Guid instanceId, MultiStageRequest request, CancellationToken cancellationToken)
            => Task.FromResult(StageResult.WithResult<MultiStageRequest, MultiStageResponse>(new MultiStageResponse { Value = "second" }, GetType()));
    }

    [Fact]
    public async Task PushWithDebugAsync_Should_Run_Each_Distinct_Custom_Stage_Its_Own_Type_Not_The_Last_Registered_One()
    {
        // Arrange: two different AddStage<T> calls for different concrete stage types on the same pipe
        // previously registered both under the same shared IPipeStage<TRequest,TResponse> DI key, so the
        // container's "last registration wins" resolution silently ran FirstStage's own slot as SecondStage
        // too — FirstStage never actually executed. Each custom stage must now resolve to its own type.
        var services = new ServiceCollection();
        services.AddConduit(c => c.RegisterPipe<MultiStageRequest, MultiStageResponse>(p =>
        {
            p.AddStage<FirstStage>();
            p.AddStage<SecondStage>();
        }), new Mock<ILog>().Object);
        var provider = services.BuildServiceProvider();
        var pipe = provider.GetRequiredService<IPipe<MultiStageRequest, MultiStageResponse>>();

        // Act
        var result = await pipe.PushWithDebugAsync(new MultiStageRequest());

        // Assert: both stages ran, each producing its own metric entry.
        Assert.Contains(result.Metrics, m => m.Name.Contains(nameof(FirstStage)));
        Assert.Contains(result.Metrics, m => m.Name.Contains(nameof(SecondStage)));
        Assert.Equal("second", result.Response!.Value);
    }
}
