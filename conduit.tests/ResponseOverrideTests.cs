using conduit.logging;
using conduit.Pipes;
using conduit.Pipes.Stages;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace conduit.tests;

public class ResponseOverrideTests
{
    public class OverrideRequest : IRequest<OverrideResponse>;

    public class OverrideResponse
    {
        public string Value { get; set; } = string.Empty;
    }

    // A default-response stage, then a handler that overrides it: two DIFFERENT stage interfaces
    // (IPipeStage<,> and IRequestHandler<,>), so each resolves to its own instance. Two AddStage<T>() calls
    // for different concrete types would instead collide on the shared IPipeStage<,> registration key — a
    // separate, pre-existing issue this test deliberately avoids rather than exercises.
    public class DefaultResponseStage(ILog logger) : PipeStage<OverrideRequest, OverrideResponse>(logger)
    {
        protected override Task<StageResult<OverrideRequest, OverrideResponse>> ExecuteInternalAsync(
            Guid instanceId, OverrideRequest request, CancellationToken cancellationToken)
            => Task.FromResult(StageResult.WithResult<OverrideRequest, OverrideResponse>(new OverrideResponse { Value = "default" }, GetType()));
    }

    public class OverrideRequestHandler(ILog logger) : RequestHandler<OverrideRequest, OverrideResponse>(logger)
    {
        public override Task<OverrideResponse> HandleAsync(OverrideRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(new OverrideResponse { Value = "handled" });
    }

    [Fact]
    public async Task PushAsync_Should_Let_A_Later_Stage_Override_An_Earlier_Stages_Response()
    {
        // Arrange: previously the pipe kept only the FIRST non-null response (`response ??= ...`), so a
        // later stage could never override it (Q6/Q12 in the edge-case review).
        var loggerMock = new Mock<ILog>();
        var services = new ServiceCollection();
        services.AddConduit(c => c.RegisterPipe<OverrideRequest, OverrideResponse>(p =>
        {
            p.AddStage<DefaultResponseStage>();
            p.AddHandler<OverrideRequestHandler>();
        }), loggerMock.Object);
        var provider = services.BuildServiceProvider();
        var pipe = provider.GetRequiredService<IPipe<OverrideRequest, OverrideResponse>>();

        // Act
        var response = await pipe.PushAsync(new OverrideRequest());

        // Assert: the handler's response replaced the earlier stage's, and exactly one override was logged.
        Assert.Equal("handled", response!.Value);
        loggerMock.Verify(l => l.Verbose(It.Is<string>(s => s.Contains("overrode previous stage's response"))), Times.Once);
    }

    [Fact]
    public async Task PushAsync_Should_Not_Log_An_Override_When_The_First_Stage_Sets_The_Response()
    {
        // Arrange
        var loggerMock = new Mock<ILog>();
        var services = new ServiceCollection();
        services.AddConduit(c => c.RegisterPipe<OverrideRequest, OverrideResponse>(p => p.AddStage<DefaultResponseStage>()), loggerMock.Object);
        var provider = services.BuildServiceProvider();
        var pipe = provider.GetRequiredService<IPipe<OverrideRequest, OverrideResponse>>();

        // Act
        var response = await pipe.PushAsync(new OverrideRequest());

        // Assert: the first stage to produce a response is not an override, so nothing should be logged.
        Assert.Equal("default", response!.Value);
        loggerMock.Verify(l => l.Verbose(It.IsAny<string>()), Times.Never);
    }
}
