using conduit.Exceptions.Handling;
using conduit.logging;
using conduit.Pipes;
using conduit.Pipes.Stages;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace conduit.tests.ExceptionHandling;

public class ReturnedExceptionDispatchTests
{
    public class ReturnedExceptionRequest : IRequest<ReturnedExceptionResponse>;

    public class ReturnedExceptionResponse
    {
        public string Value { get; set; } = string.Empty;
    }

    public class ReturningStage(ILog logger) : PipeStage<ReturnedExceptionRequest, ReturnedExceptionResponse>(logger)
    {
        protected override Task<StageResult<ReturnedExceptionRequest, ReturnedExceptionResponse>> ExecuteInternalAsync(
            Guid instanceId, ReturnedExceptionRequest request, CancellationToken cancellationToken)
            => Task.FromResult(StageResult.WithException<ReturnedExceptionRequest, ReturnedExceptionResponse>(
                new InvalidOperationException("reported, not thrown"), GetType()));
    }

    public class BusinessHandler(ILog logger) : RequestHandler<ReturnedExceptionRequest, ReturnedExceptionResponse>(logger)
    {
        public static int InvocationCount;

        public override Task<ReturnedExceptionResponse> HandleAsync(ReturnedExceptionRequest request, CancellationToken cancellationToken = default)
        {
            InvocationCount++;
            return Task.FromResult(new ReturnedExceptionResponse { Value = "business-logic-ran" });
        }
    }

    public class RecoveringStageHandler : IStageExceptionHandler<ReturnedExceptionRequest, ReturnedExceptionResponse, InvalidOperationException>
    {
        public int Order => 0;

        public Task HandleAsync(
            ReturnedExceptionRequest request,
            InvalidOperationException exception,
            RequestExceptionHandlerState<ReturnedExceptionResponse> state,
            CancellationToken cancellationToken)
        {
            state.SetHandled(new ReturnedExceptionResponse { Value = "recovered-from-returned-exception" });
            return Task.CompletedTask;
        }
    }

    public class NonRecoveringStageHandler : IStageExceptionHandler<ReturnedExceptionRequest, ReturnedExceptionResponse, InvalidOperationException>
    {
        public int Order => 0;

        public Task HandleAsync(
            ReturnedExceptionRequest request,
            InvalidOperationException exception,
            RequestExceptionHandlerState<ReturnedExceptionResponse> state,
            CancellationToken cancellationToken)
            => Task.CompletedTask; // never calls SetHandled
    }

    [Fact]
    public async Task A_Stage_That_Returns_StageResult_WithException_Should_Still_Reach_Dispatch_When_Handled()
    {
        // Arrange: ReturningStage never throws — it reports its failure via StageResult.WithException, a
        // legitimate alternate failure-reporting style. Dispatch must still see the ORIGINAL exception.
        BusinessHandler.InvocationCount = 0;
        var services = new ServiceCollection();
        services.AddConduit(c => c.RegisterPipe<ReturnedExceptionRequest, ReturnedExceptionResponse>(p =>
        {
            p.AddStage<ReturningStage>();
            p.AddHandler<BusinessHandler>();
        }), new Mock<ILog>().Object);
        services.AddTransient<
            IStageExceptionHandler<ReturnedExceptionRequest, ReturnedExceptionResponse, InvalidOperationException>,
            RecoveringStageHandler>();
        var provider = services.BuildServiceProvider();
        var pipe = provider.GetRequiredService<IPipe<ReturnedExceptionRequest, ReturnedExceptionResponse>>();

        // Act
        var result = await pipe.PushWithDebugAsync(new ReturnedExceptionRequest());

        // Assert
        Assert.Equal("recovered-from-returned-exception", result.Response!.Value);
        Assert.True(result.ShortCircuited);
        Assert.Equal(0, BusinessHandler.InvocationCount);
    }

    [Fact]
    public async Task A_Stage_That_Returns_StageResult_WithException_Should_Fall_Through_To_StageFailedException_When_Not_Handled()
    {
        // Arrange: proves the fix doesn't just special-case the happy path — an unhandled returned
        // exception must still surface as StageFailedException, exactly like today.
        var services = new ServiceCollection();
        services.AddConduit(c => c.RegisterPipe<ReturnedExceptionRequest, ReturnedExceptionResponse>(p =>
        {
            p.AddStage<ReturningStage>();
            p.AddHandler<BusinessHandler>();
        }), new Mock<ILog>().Object);
        services.AddTransient<
            IStageExceptionHandler<ReturnedExceptionRequest, ReturnedExceptionResponse, InvalidOperationException>,
            NonRecoveringStageHandler>();
        var provider = services.BuildServiceProvider();
        var pipe = provider.GetRequiredService<IPipe<ReturnedExceptionRequest, ReturnedExceptionResponse>>();

        // Act
        Func<Task> act = () => pipe.PushAsync(new ReturnedExceptionRequest());

        // Assert
        var exception = await Assert.ThrowsAsync<conduit.Exceptions.StageFailedException>(act);
        Assert.IsType<InvalidOperationException>(exception.InnerException);
    }
}
