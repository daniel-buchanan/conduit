using conduit.Exceptions;
using conduit.logging;
using conduit.Pipes;
using conduit.Pipes.Stages;
using conduit.validation;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace conduit.tests;

public class PipeExceptionWrappingTests
{
    public class ThrowingRequest : IRequest<ThrowingResponse>;

    public class ThrowingResponse;

    public class ArbitraryThrowingStage(ILog logger) : PipeStage<ThrowingRequest, ThrowingResponse>(logger)
    {
        protected override Task<StageResult<ThrowingRequest, ThrowingResponse>> ExecuteInternalAsync(
            Guid instanceId, ThrowingRequest request, CancellationToken cancellationToken)
            => throw new InvalidOperationException("boom");
    }

    public class ValidatorNotFoundThrowingStage(ILog logger) : PipeStage<ThrowingRequest, ThrowingResponse>(logger)
    {
        protected override Task<StageResult<ThrowingRequest, ThrowingResponse>> ExecuteInternalAsync(
            Guid instanceId, ThrowingRequest request, CancellationToken cancellationToken)
            => throw new ValidatorNotFoundException("no validator registered");
    }

    public class StageFailedThrowingStage(ILog logger) : PipeStage<ThrowingRequest, ThrowingResponse>(logger)
    {
        protected override Task<StageResult<ThrowingRequest, ThrowingResponse>> ExecuteInternalAsync(
            Guid instanceId, ThrowingRequest request, CancellationToken cancellationToken)
            => throw new StageFailedException("already wrapped upstream");
    }

    private static IPipe<ThrowingRequest, ThrowingResponse> BuildPipeWith<TStage>()
        where TStage : class, IPipeStage<ThrowingRequest, ThrowingResponse>
    {
        var services = new ServiceCollection();
        services.AddConduit(c => c.RegisterPipe<ThrowingRequest, ThrowingResponse>(p => p.AddStage<TStage>()), new Mock<ILog>().Object);
        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IPipe<ThrowingRequest, ThrowingResponse>>();
    }

    [Fact]
    public async Task An_Unexpected_Exception_From_A_Stage_Should_Be_Wrapped_As_StageFailedException()
    {
        // Arrange: previously this exception bubbled up raw and unwrapped (Q5/Q10 in the edge-case review).
        var pipe = BuildPipeWith<ArbitraryThrowingStage>();

        // Act
        Func<Task> act = () => pipe.PushAsync(new ThrowingRequest());

        // Assert
        var exception = await Assert.ThrowsAsync<StageFailedException>(act);
        Assert.IsType<InvalidOperationException>(exception.InnerException);
        Assert.Equal("boom", exception.InnerException!.Message);
    }

    [Fact]
    public async Task A_ValidatorNotFoundException_From_A_Stage_Should_Pass_Through_Unwrapped()
    {
        // Arrange: ValidatorNotFoundException must surface as its own distinct error, not a generic
        // pipeline-stage failure.
        var pipe = BuildPipeWith<ValidatorNotFoundThrowingStage>();

        // Act
        Func<Task> act = () => pipe.PushAsync(new ThrowingRequest());

        // Assert
        await Assert.ThrowsAsync<ValidatorNotFoundException>(act);
    }

    [Fact]
    public async Task A_StageFailedException_From_A_Stage_Should_Not_Be_Double_Wrapped()
    {
        // Arrange
        var pipe = BuildPipeWith<StageFailedThrowingStage>();

        // Act
        Func<Task> act = () => pipe.PushAsync(new ThrowingRequest());

        // Assert
        var exception = await Assert.ThrowsAsync<StageFailedException>(act);
        Assert.Equal("already wrapped upstream", exception.Message);
        Assert.Null(exception.InnerException);
    }
}
