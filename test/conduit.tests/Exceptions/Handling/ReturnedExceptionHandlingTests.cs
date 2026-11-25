using conduit.Exceptions.Handling;
using conduit.logging;
using conduit.Pipes;
using conduit.Pipes.Stages;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace conduit.tests.Exceptions.Handling;

public class ReturnedExceptionHandlingTests
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
            ExceptionHandlerState<ReturnedExceptionResponse> state,
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
            ExceptionHandlerState<ReturnedExceptionResponse> state,
            CancellationToken cancellationToken)
            => Task.CompletedTask; // never calls SetHandled
    }

    [Fact]
    public async Task A_Stage_That_Returns_StageResult_WithException_Should_Still_Reach_Exception_Handlers_When_Handled()
    {
        // Arrange: ReturningStage never throws — it reports its failure via StageResult.WithException, a
        // legitimate alternate failure-reporting style. Exception handlers must still see the ORIGINAL exception.
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

    public class ConfigurableReturningStage(ILog logger) : PipeStage<ReturnedExceptionRequest, ReturnedExceptionResponse>(logger)
    {
        public static Exception ToReturn = new InvalidOperationException();

        protected override Task<StageResult<ReturnedExceptionRequest, ReturnedExceptionResponse>> ExecuteInternalAsync(
            Guid instanceId, ReturnedExceptionRequest request, CancellationToken cancellationToken)
            => Task.FromResult(StageResult.WithException<ReturnedExceptionRequest, ReturnedExceptionResponse>(ToReturn, GetType()));
    }

    public class CountingAction : IStageExceptionAction<ReturnedExceptionRequest, Exception>
    {
        public static int InvocationCount;
        public int Order => 0;

        public Task ExecuteAsync(ReturnedExceptionRequest request, Exception exception, CancellationToken cancellationToken)
        {
            InvocationCount++;
            return Task.CompletedTask;
        }
    }

    public class CountingCatchAllHandler : IStageExceptionHandler<ReturnedExceptionRequest, ReturnedExceptionResponse, Exception>
    {
        public static int InvocationCount;
        public int Order => 0;

        public Task HandleAsync(
            ReturnedExceptionRequest request,
            Exception exception,
            ExceptionHandlerState<ReturnedExceptionResponse> state,
            CancellationToken cancellationToken)
        {
            InvocationCount++;
            return Task.CompletedTask;
        }
    }

    public class BrokenStageHandler : IStageExceptionHandler<ReturnedExceptionRequest, ReturnedExceptionResponse, InvalidOperationException>
    {
        public int Order => 0;

        public Task HandleAsync(
            ReturnedExceptionRequest request,
            InvalidOperationException exception,
            ExceptionHandlerState<ReturnedExceptionResponse> state,
            CancellationToken cancellationToken)
            => throw new NotSupportedException("handler itself is broken");
    }

    private static IPipe<ReturnedExceptionRequest, ReturnedExceptionResponse> BuildConfigurablePipe(Exception toReturn, Action<IServiceCollection> configure)
    {
        ConfigurableReturningStage.ToReturn = toReturn;
        CountingAction.InvocationCount = 0;
        CountingCatchAllHandler.InvocationCount = 0;
        var services = new ServiceCollection();
        services.AddConduit(c => c.RegisterPipe<ReturnedExceptionRequest, ReturnedExceptionResponse>(p =>
        {
            p.AddStage<ConfigurableReturningStage>();
            p.AddHandler<BusinessHandler>();
        }), new Mock<ILog>().Object);
        configure(services);
        return services.BuildServiceProvider().GetRequiredService<IPipe<ReturnedExceptionRequest, ReturnedExceptionResponse>>();
    }

    [Fact]
    public async Task A_Handler_That_Throws_On_A_Returned_Exception_Should_Propagate_Directly_And_Run_Actions_Once()
    {
        // Arrange
        var pipe = BuildConfigurablePipe(new InvalidOperationException("reported"), s =>
        {
            s.AddTransient<IStageExceptionAction<ReturnedExceptionRequest, Exception>, CountingAction>();
            s.AddTransient<IStageExceptionHandler<ReturnedExceptionRequest, ReturnedExceptionResponse, InvalidOperationException>, BrokenStageHandler>();
        });

        // Act
        Func<Task> act = () => pipe.PushAsync(new ReturnedExceptionRequest());

        // Assert: the handler's own exception surfaces raw, and the failure is not sent to the handlers a second time.
        await Assert.ThrowsAsync<NotSupportedException>(act);
        Assert.Equal(1, CountingAction.InvocationCount);
    }

    [Fact]
    public async Task A_Returned_Passthrough_Exception_Should_Skip_Exception_Handlers()
    {
        // Arrange
        var passthrough = new conduit.Exceptions.StageFailedException("already wrapped", new InvalidOperationException());
        var pipe = BuildConfigurablePipe(passthrough, s =>
            s.AddTransient<IStageExceptionHandler<ReturnedExceptionRequest, ReturnedExceptionResponse, Exception>, CountingCatchAllHandler>());

        // Act
        Func<Task> act = () => pipe.PushAsync(new ReturnedExceptionRequest());

        // Assert
        var thrown = await Assert.ThrowsAsync<conduit.Exceptions.StageFailedException>(act);
        Assert.Same(passthrough, thrown);
        Assert.Equal(0, CountingCatchAllHandler.InvocationCount);
    }

    [Fact]
    public async Task A_Returned_Cancellation_For_A_Cancelled_Caller_Should_Pass_Through_Unwrapped_And_Skip_Exception_Handlers()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var pipe = BuildConfigurablePipe(new OperationCanceledException(cts.Token), s =>
            s.AddTransient<IStageExceptionHandler<ReturnedExceptionRequest, ReturnedExceptionResponse, Exception>, CountingCatchAllHandler>());

        // Act
        Func<Task> act = () => pipe.PushAsync(new ReturnedExceptionRequest(), cts.Token);

        // Assert
        await Assert.ThrowsAsync<OperationCanceledException>(act);
        Assert.Equal(0, CountingCatchAllHandler.InvocationCount);
    }
}
