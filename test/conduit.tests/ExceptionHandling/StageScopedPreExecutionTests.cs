using conduit.Exceptions.Handling;
using conduit.logging;
using conduit.Pipes;
using conduit.Pipes.Stages;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace conduit.tests.ExceptionHandling;

public class StageScopedPreExecutionTests
{
    public class PreStageRequest : IRequest<PreStageResponse>;

    public class PreStageResponse
    {
        public string Value { get; set; } = string.Empty;
    }

    public class ThrowingPreStage(ILog logger) : PipeStage<PreStageRequest, PreStageResponse>(logger)
    {
        protected override Task<StageResult<PreStageRequest, PreStageResponse>> ExecuteInternalAsync(
            Guid instanceId, PreStageRequest request, CancellationToken cancellationToken)
            => throw new UnauthorizedAccessException("not authorized");
    }

    public class BusinessHandler(ILog logger) : RequestHandler<PreStageRequest, PreStageResponse>(logger)
    {
        public static int InvocationCount;

        public override Task<PreStageResponse> HandleAsync(PreStageRequest request, CancellationToken cancellationToken = default)
        {
            InvocationCount++;
            return Task.FromResult(new PreStageResponse { Value = "business-logic-ran" });
        }
    }

    public class DenyingStageHandler : IStageExceptionHandler<PreStageRequest, PreStageResponse, UnauthorizedAccessException>
    {
        public int Order => 0;

        public Task HandleAsync(
            PreStageRequest request,
            UnauthorizedAccessException exception,
            RequestExceptionHandlerState<PreStageResponse> state,
            CancellationToken cancellationToken)
        {
            state.SetHandled(new PreStageResponse { Value = "not-authorized" });
            return Task.CompletedTask;
        }
    }

    public class AuditAction : IStageExceptionAction<PreStageRequest, UnauthorizedAccessException>
    {
        public static int InvocationCount;
        public int Order => 0;

        public Task ExecuteAsync(PreStageRequest request, UnauthorizedAccessException exception, CancellationToken cancellationToken)
        {
            InvocationCount++;
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task A_Handled_PreExecution_Stage_Exception_Should_ShortCircuit_Before_The_Handler_Runs()
    {
        // Arrange: the scenario from the design discussion — a failed auth check must not let the real
        // business-logic Handler run afterward just because the flat loop would otherwise continue to it.
        BusinessHandler.InvocationCount = 0;
        AuditAction.InvocationCount = 0;
        var services = new ServiceCollection();
        services.AddConduit(c => c.RegisterPipe<PreStageRequest, PreStageResponse>(p =>
        {
            p.AddStage<ThrowingPreStage>();
            p.AddHandler<BusinessHandler>();
        }), new Mock<ILog>().Object);
        services.AddTransient<IStageExceptionHandler<PreStageRequest, PreStageResponse, UnauthorizedAccessException>, DenyingStageHandler>();
        services.AddTransient<IStageExceptionAction<PreStageRequest, UnauthorizedAccessException>, AuditAction>();
        var provider = services.BuildServiceProvider();
        var pipe = provider.GetRequiredService<IPipe<PreStageRequest, PreStageResponse>>();

        // Act
        var result = await pipe.PushWithDebugAsync(new PreStageRequest());

        // Assert
        Assert.Equal("not-authorized", result.Response!.Value);
        Assert.True(result.ShortCircuited);
        Assert.Equal(0, BusinessHandler.InvocationCount);
        Assert.Equal(1, AuditAction.InvocationCount);
    }
}
