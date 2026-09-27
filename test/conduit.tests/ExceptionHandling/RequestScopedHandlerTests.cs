using conduit.Exceptions.Handling;
using conduit.logging;
using conduit.Pipes;
using conduit.Pipes.Stages;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace conduit.tests.ExceptionHandling;

public class RequestScopedHandlerTests
{
    public class HandlerExceptionRequest : IRequest<HandlerExceptionResponse>;

    public class HandlerExceptionResponse
    {
        public string Value { get; set; } = string.Empty;
    }

    public class ThrowingHandler(ILog logger) : RequestHandler<HandlerExceptionRequest, HandlerExceptionResponse>(logger)
    {
        public override Task<HandlerExceptionResponse> HandleAsync(HandlerExceptionRequest request, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("handler boom");
    }

    public class RecoveringHandler : IRequestExceptionHandler<HandlerExceptionRequest, HandlerExceptionResponse, InvalidOperationException>
    {
        public int Order => 0;

        public Task HandleAsync(
            HandlerExceptionRequest request,
            InvalidOperationException exception,
            RequestExceptionHandlerState<HandlerExceptionResponse> state,
            CancellationToken cancellationToken)
        {
            state.SetHandled(new HandlerExceptionResponse { Value = "recovered" });
            return Task.CompletedTask;
        }
    }

    public class NonRecoveringHandler : IRequestExceptionHandler<HandlerExceptionRequest, HandlerExceptionResponse, InvalidOperationException>
    {
        public int Order => 0;

        public Task HandleAsync(
            HandlerExceptionRequest request,
            InvalidOperationException exception,
            RequestExceptionHandlerState<HandlerExceptionResponse> state,
            CancellationToken cancellationToken)
            => Task.CompletedTask; // never calls SetHandled
    }

    public class LoggingPostStage(ILog logger) : PipeStage<HandlerExceptionRequest, HandlerExceptionResponse>(logger)
    {
        public static int InvocationCount;

        protected override Task<StageResult<HandlerExceptionRequest, HandlerExceptionResponse>> ExecuteInternalAsync(
            Guid instanceId, HandlerExceptionRequest request, CancellationToken cancellationToken)
        {
            InvocationCount++;
            return Task.FromResult(StageResult.WithIndeterminateResult<HandlerExceptionRequest, HandlerExceptionResponse>(GetType()));
        }
    }

    public class NonThrowingHandler(ILog logger) : RequestHandler<HandlerExceptionRequest, HandlerExceptionResponse>(logger)
    {
        public override Task<HandlerExceptionResponse> HandleAsync(HandlerExceptionRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(new HandlerExceptionResponse { Value = "normal" });
    }

    private sealed class DelegateHandler(
        Func<HandlerExceptionRequest, InvalidOperationException, RequestExceptionHandlerState<HandlerExceptionResponse>, CancellationToken, Task> handle)
        : IRequestExceptionHandler<HandlerExceptionRequest, HandlerExceptionResponse, InvalidOperationException>
    {
        public int Order => 0;

        public Task HandleAsync(
            HandlerExceptionRequest request,
            InvalidOperationException exception,
            RequestExceptionHandlerState<HandlerExceptionResponse> state,
            CancellationToken cancellationToken)
            => handle(request, exception, state, cancellationToken);
    }

    private static IPipe<HandlerExceptionRequest, HandlerExceptionResponse> BuildPipe(Action<IServiceCollection> configureHandlers)
    {
        var services = new ServiceCollection();
        services.AddConduit(c => c.RegisterPipe<HandlerExceptionRequest, HandlerExceptionResponse>(p =>
        {
            p.AddHandler<ThrowingHandler>();
            p.AddStage<LoggingPostStage>();
        }), new Mock<ILog>().Object);
        configureHandlers(services);
        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IPipe<HandlerExceptionRequest, HandlerExceptionResponse>>();
    }

    [Fact]
    public async Task A_Handler_That_Calls_SetHandled_Should_ShortCircuit_And_Return_Its_Response()
    {
        // Arrange
        LoggingPostStage.InvocationCount = 0;
        var pipe = BuildPipe(s => s.AddTransient<
            IRequestExceptionHandler<HandlerExceptionRequest, HandlerExceptionResponse, InvalidOperationException>,
            RecoveringHandler>());

        // Act
        var result = await pipe.PushWithDebugAsync(new HandlerExceptionRequest());

        // Assert: handled, short-circuited, post-stage never ran, metrics truncated to just the Handler.
        Assert.Equal("recovered", result.Response!.Value);
        Assert.True(result.ShortCircuited);
        Assert.Single(result.Metrics);
        Assert.Equal(0, LoggingPostStage.InvocationCount);
    }

    [Fact]
    public async Task A_Handler_That_Never_Calls_SetHandled_Should_Fall_Through_To_StageFailedException()
    {
        // Arrange
        var pipe = BuildPipe(s => s.AddTransient<
            IRequestExceptionHandler<HandlerExceptionRequest, HandlerExceptionResponse, InvalidOperationException>,
            NonRecoveringHandler>());

        // Act
        Func<Task> act = () => pipe.PushAsync(new HandlerExceptionRequest());

        // Assert
        var exception = await Assert.ThrowsAsync<conduit.Exceptions.StageFailedException>(act);
        Assert.IsType<InvalidOperationException>(exception.InnerException);
    }

    [Fact]
    public async Task The_CancellationToken_Passed_To_PushAsync_Should_Reach_The_Registered_Handler()
    {
        // Arrange
        CancellationToken? observedToken = null;
        var pipe = BuildPipe(s => s.AddTransient<
            IRequestExceptionHandler<HandlerExceptionRequest, HandlerExceptionResponse, InvalidOperationException>>(_ =>
            new DelegateHandler((req, ex, state, token) =>
            {
                observedToken = token;
                state.SetHandled(new HandlerExceptionResponse { Value = "recovered" });
                return Task.CompletedTask;
            })));
        using var cts = new CancellationTokenSource();

        // Act
        await pipe.PushAsync(new HandlerExceptionRequest(), cts.Token);

        // Assert
        Assert.Equal(cts.Token, observedToken);
    }

    [Fact]
    public async Task A_Normal_Pipe_Run_With_No_Exception_Should_Report_ShortCircuited_False()
    {
        // Arrange: regression guard — introducing ShortCircuited must not make it true by default.
        var services = new ServiceCollection();
        services.AddConduit(c => c.RegisterPipe<HandlerExceptionRequest, HandlerExceptionResponse>(p =>
            p.AddHandler<NonThrowingHandler>()), new Mock<ILog>().Object);
        var provider = services.BuildServiceProvider();
        var pipe = provider.GetRequiredService<IPipe<HandlerExceptionRequest, HandlerExceptionResponse>>();

        // Act
        var result = await pipe.PushWithDebugAsync(new HandlerExceptionRequest());

        // Assert
        Assert.Equal("normal", result.Response!.Value);
        Assert.False(result.ShortCircuited);
    }
}
