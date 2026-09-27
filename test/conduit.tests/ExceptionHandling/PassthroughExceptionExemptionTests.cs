using conduit.Exceptions.Handling;
using conduit.logging;
using conduit.Pipes;
using conduit.Pipes.Stages;
using conduit.validation;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace conduit.tests.ExceptionHandling;

public class PassthroughExceptionExemptionTests
{
    public class PassthroughRequest : IRequest<PassthroughResponse>;

    public class PassthroughResponse;

    public class ValidatorNotFoundThrowingStage(ILog logger) : PipeStage<PassthroughRequest, PassthroughResponse>(logger)
    {
        protected override Task<StageResult<PassthroughRequest, PassthroughResponse>> ExecuteInternalAsync(
            Guid instanceId, PassthroughRequest request, CancellationToken cancellationToken)
            => throw new ValidatorNotFoundException("no validator registered");
    }

    public class NeverCalledHandler : IStageExceptionHandler<PassthroughRequest, PassthroughResponse, ValidatorNotFoundException>
    {
        public static int InvocationCount;
        public int Order => 0;

        public Task HandleAsync(
            PassthroughRequest request,
            ValidatorNotFoundException exception,
            RequestExceptionHandlerState<PassthroughResponse> state,
            CancellationToken cancellationToken)
        {
            InvocationCount++;
            state.SetHandled(new PassthroughResponse());
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task A_ValidatorNotFoundException_Should_Bypass_Dispatch_Even_When_A_Matching_Handler_Is_Registered()
    {
        // Arrange
        NeverCalledHandler.InvocationCount = 0;
        var services = new ServiceCollection();
        services.AddConduit(c => c.RegisterPipe<PassthroughRequest, PassthroughResponse>(p => p.AddStage<ValidatorNotFoundThrowingStage>()), new Mock<ILog>().Object);
        services.AddTransient<IStageExceptionHandler<PassthroughRequest, PassthroughResponse, ValidatorNotFoundException>, NeverCalledHandler>();
        var provider = services.BuildServiceProvider();
        var pipe = provider.GetRequiredService<IPipe<PassthroughRequest, PassthroughResponse>>();

        // Act
        Func<Task> act = () => pipe.PushAsync(new PassthroughRequest());

        // Assert: exception passes through unwrapped, and the registered handler was never even invoked.
        await Assert.ThrowsAsync<ValidatorNotFoundException>(act);
        Assert.Equal(0, NeverCalledHandler.InvocationCount);
    }
}
