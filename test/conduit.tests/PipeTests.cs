using conduit.logging;
using conduit.tests.Handlers;
using conduit.tests.Stages;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace conduit.tests;

public class PipeTests
{
    private readonly IServiceProvider _provider;
    private readonly Mock<ILog> _loggerMock;

    public PipeTests()
    {
        _loggerMock = new Mock<ILog>();
        var services = new ServiceCollection();
        services.AddConduit(c =>
        {
            c.RegisterPipe<TestRequest, TestResponse>(pb =>
            {
                pb.AddStage<LoggingStage<TestRequest, TestResponse>>();
                pb.AddHandler<TestRequestHandler>();
            });
        }, _loggerMock.Object);
        _provider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task ManualPipeSucceeds()
    {
        // Act
        var conduit = _provider.GetRequiredService<IConduit>();
        var response = await conduit.PushAsync(new TestRequest(), CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
    }
    
    [Fact]
    public async Task LoggingStageHit()
    {
        // Act
        var conduit = _provider.GetRequiredService<IConduit>();
        var response = await conduit.PushAsync(new TestRequest(), CancellationToken.None);

        // Assert: matches LoggingStage's own explicit log line specifically — the framework's per-stage
        // "Executing stage ..." log also mentions the stage's name, so a looser Contains("LoggingStage")
        // filter would double-count once the stage resolves to its own concrete type (see StageDescriptor).
        _loggerMock.Verify(l => l.Debug(It.Is<string>(s => s.Contains("[LoggingStage] ::"))), Times.Once);
        response.Should().NotBeNull();
    }
    
    [Fact]
    public async Task RequestHandlerHit()
    {
        // Act
        var conduit = _provider.GetRequiredService<IConduit>();
        var response = await conduit.PushAsync(new TestRequest(), CancellationToken.None);

        // Assert
        _loggerMock.Verify(l => l.Verbose(It.Is<string>(s => s.Contains(nameof(TestRequestHandler)))), Times.Once);
        response.Should().NotBeNull();
    }
    
    [Fact]
    public async Task PushWithDebugAsync_Reports_A_Metric_Per_Stage()
    {
        // Act
        var conduit = _provider.GetRequiredService<IConduit>();
        var response = await conduit.PushWithDebugAsync(new TestRequest(), CancellationToken.None);

        // Assert: timing values depend on the machine, so only their presence and ordering are checked.
        response.Response.Should().NotBeNull();
        response.OverallDurationMs.Should().BeGreaterThanOrEqualTo(0);
        response.Metrics.Select(m => m.Index).Should().Equal(0, 1);
    }
}

