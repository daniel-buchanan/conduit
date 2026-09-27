using conduit.Exceptions.Handling;
using conduit.logging;
using conduit.Pipes;
using conduit.Pipes.Stages;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace conduit.tests.ExceptionHandling;

/// <summary>
/// Pins that Request-scoped (Handler-only) and Stage-scoped (everything else) exception dispatch are
/// mutually exclusive: registering the wrong-scope interface for a given stage must never fire, regardless
/// of exception type match. This guards the <c>stage is IRequestHandler</c> discriminator in
/// <see cref="Pipe{TRequest,TResponse}.ExecuteStage"/> and the interface selection in
/// <see cref="ExceptionHandlerDispatcher.DispatchAsync{TRequest,TResponse}"/>.
/// </summary>
public class ScopeIsolationTests
{
    public class ScopeIsolationRequest : IRequest<ScopeIsolationResponse>;

    public class ScopeIsolationResponse
    {
        public string Value { get; set; } = string.Empty;
    }

    public class ThrowingNonHandlerStage(ILog logger) : PipeStage<ScopeIsolationRequest, ScopeIsolationResponse>(logger)
    {
        protected override Task<StageResult<ScopeIsolationRequest, ScopeIsolationResponse>> ExecuteInternalAsync(
            Guid instanceId, ScopeIsolationRequest request, CancellationToken cancellationToken)
            => throw new InvalidOperationException("non-handler stage boom");
    }

    public class ThrowingHandler(ILog logger) : RequestHandler<ScopeIsolationRequest, ScopeIsolationResponse>(logger)
    {
        public override Task<ScopeIsolationResponse> HandleAsync(ScopeIsolationRequest request, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("handler boom");
    }

    public class CorrectStageHandler : IStageExceptionHandler<ScopeIsolationRequest, ScopeIsolationResponse, InvalidOperationException>
    {
        public int Order => 0;

        public Task HandleAsync(
            ScopeIsolationRequest request,
            InvalidOperationException exception,
            RequestExceptionHandlerState<ScopeIsolationResponse> state,
            CancellationToken cancellationToken)
        {
            state.SetHandled(new ScopeIsolationResponse { Value = "stage-scoped-handled" });
            return Task.CompletedTask;
        }
    }

    public class WrongScopeRequestHandler : IRequestExceptionHandler<ScopeIsolationRequest, ScopeIsolationResponse, InvalidOperationException>
    {
        public static int InvocationCount;
        public int Order => 0;

        public Task HandleAsync(
            ScopeIsolationRequest request,
            InvalidOperationException exception,
            RequestExceptionHandlerState<ScopeIsolationResponse> state,
            CancellationToken cancellationToken)
        {
            InvocationCount++;
            state.SetHandled(new ScopeIsolationResponse { Value = "wrongly-handled-by-request-scope" });
            return Task.CompletedTask;
        }
    }

    public class CorrectRequestHandler : IRequestExceptionHandler<ScopeIsolationRequest, ScopeIsolationResponse, InvalidOperationException>
    {
        public int Order => 0;

        public Task HandleAsync(
            ScopeIsolationRequest request,
            InvalidOperationException exception,
            RequestExceptionHandlerState<ScopeIsolationResponse> state,
            CancellationToken cancellationToken)
        {
            state.SetHandled(new ScopeIsolationResponse { Value = "request-scoped-handled" });
            return Task.CompletedTask;
        }
    }

    public class WrongScopeStageHandler : IStageExceptionHandler<ScopeIsolationRequest, ScopeIsolationResponse, InvalidOperationException>
    {
        public static int InvocationCount;
        public int Order => 0;

        public Task HandleAsync(
            ScopeIsolationRequest request,
            InvalidOperationException exception,
            RequestExceptionHandlerState<ScopeIsolationResponse> state,
            CancellationToken cancellationToken)
        {
            InvocationCount++;
            state.SetHandled(new ScopeIsolationResponse { Value = "wrongly-handled-by-stage-scope" });
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task A_NonHandler_Stage_Exception_Should_Only_Be_Seen_By_The_Stage_Scoped_Handler()
    {
        // Arrange
        WrongScopeRequestHandler.InvocationCount = 0;
        var services = new ServiceCollection();
        services.AddConduit(c => c.RegisterPipe<ScopeIsolationRequest, ScopeIsolationResponse>(p =>
        {
            p.AddStage<ThrowingNonHandlerStage>();
            p.AddHandler<ThrowingHandler>();
        }), new Mock<ILog>().Object);
        services.AddTransient<
            IStageExceptionHandler<ScopeIsolationRequest, ScopeIsolationResponse, InvalidOperationException>,
            CorrectStageHandler>();
        services.AddTransient<
            IRequestExceptionHandler<ScopeIsolationRequest, ScopeIsolationResponse, InvalidOperationException>,
            WrongScopeRequestHandler>();
        var provider = services.BuildServiceProvider();
        var pipe = provider.GetRequiredService<IPipe<ScopeIsolationRequest, ScopeIsolationResponse>>();

        // Act
        var response = await pipe.PushAsync(new ScopeIsolationRequest());

        // Assert
        Assert.Equal("stage-scoped-handled", response!.Value);
        Assert.Equal(0, WrongScopeRequestHandler.InvocationCount);
    }

    [Fact]
    public async Task A_Handler_Stage_Exception_Should_Only_Be_Seen_By_The_Request_Scoped_Handler()
    {
        // Arrange
        WrongScopeStageHandler.InvocationCount = 0;
        var services = new ServiceCollection();
        services.AddConduit(c => c.RegisterPipe<ScopeIsolationRequest, ScopeIsolationResponse>(p =>
            p.AddHandler<ThrowingHandler>()), new Mock<ILog>().Object);
        services.AddTransient<
            IRequestExceptionHandler<ScopeIsolationRequest, ScopeIsolationResponse, InvalidOperationException>,
            CorrectRequestHandler>();
        services.AddTransient<
            IStageExceptionHandler<ScopeIsolationRequest, ScopeIsolationResponse, InvalidOperationException>,
            WrongScopeStageHandler>();
        var provider = services.BuildServiceProvider();
        var pipe = provider.GetRequiredService<IPipe<ScopeIsolationRequest, ScopeIsolationResponse>>();

        // Act
        var response = await pipe.PushAsync(new ScopeIsolationRequest());

        // Assert
        Assert.Equal("request-scoped-handled", response!.Value);
        Assert.Equal(0, WrongScopeStageHandler.InvocationCount);
    }
}
