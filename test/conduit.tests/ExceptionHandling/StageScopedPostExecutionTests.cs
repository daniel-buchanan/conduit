using conduit.Exceptions.Handling;
using conduit.logging;
using conduit.Pipes;
using conduit.Pipes.Stages;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace conduit.tests.ExceptionHandling;

public class StageScopedPostExecutionTests
{
    public class PostStageRequest : IRequest<PostStageResponse>;

    public class PostStageResponse
    {
        public string Value { get; set; } = string.Empty;
    }

    public class SucceedingHandler(ILog logger) : RequestHandler<PostStageRequest, PostStageResponse>(logger)
    {
        public override Task<PostStageResponse> HandleAsync(PostStageRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(new PostStageResponse { Value = "handled-ok" });
    }

    public class ThrowingPostStage(ILog logger) : PipeStage<PostStageRequest, PostStageResponse>(logger)
    {
        protected override Task<StageResult<PostStageRequest, PostStageResponse>> ExecuteInternalAsync(
            Guid instanceId, PostStageRequest request, CancellationToken cancellationToken)
            => throw new TimeoutException("post-stage timed out");
    }

    public class LaterPostStage(ILog logger) : PipeStage<PostStageRequest, PostStageResponse>(logger)
    {
        public static int InvocationCount;

        protected override Task<StageResult<PostStageRequest, PostStageResponse>> ExecuteInternalAsync(
            Guid instanceId, PostStageRequest request, CancellationToken cancellationToken)
        {
            InvocationCount++;
            return Task.FromResult(StageResult.WithIndeterminateResult<PostStageRequest, PostStageResponse>(GetType()));
        }
    }

    public class TimeoutRecoveryHandler : IStageExceptionHandler<PostStageRequest, PostStageResponse, TimeoutException>
    {
        public int Order => 0;

        public Task HandleAsync(
            PostStageRequest request,
            TimeoutException exception,
            RequestExceptionHandlerState<PostStageResponse> state,
            CancellationToken cancellationToken)
        {
            state.SetHandled(new PostStageResponse { Value = "recovered-from-timeout" });
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task A_Handled_PostExecution_Stage_Exception_Should_Skip_Every_Remaining_Stage()
    {
        // Arrange: the Handler already succeeded; a post-stage after it then throws and is handled. The
        // short-circuit rule is uniform (ADR-0016) — a LATER post-stage must not run either.
        LaterPostStage.InvocationCount = 0;
        var services = new ServiceCollection();
        services.AddConduit(c => c.RegisterPipe<PostStageRequest, PostStageResponse>(p =>
        {
            p.AddHandler<SucceedingHandler>();
            p.AddStage<ThrowingPostStage>();
            p.AddStage<LaterPostStage>();
        }), new Mock<ILog>().Object);
        services.AddTransient<IStageExceptionHandler<PostStageRequest, PostStageResponse, TimeoutException>, TimeoutRecoveryHandler>();
        var provider = services.BuildServiceProvider();
        var pipe = provider.GetRequiredService<IPipe<PostStageRequest, PostStageResponse>>();

        // Act
        var result = await pipe.PushWithDebugAsync(new PostStageRequest());

        // Assert
        Assert.Equal("recovered-from-timeout", result.Response!.Value);
        Assert.True(result.ShortCircuited);
        Assert.Equal(0, LaterPostStage.InvocationCount);
        Assert.Equal(2, result.Metrics.Length); // SucceedingHandler + ThrowingPostStage only, LaterPostStage excluded
    }
}
