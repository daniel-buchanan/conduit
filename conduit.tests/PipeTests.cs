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
                pb.AddHandler<TestHandler>();
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

        // Assert
        _loggerMock.Verify(l => l.Debug(It.Is<string>(s => s.Contains("LoggingStage"))), Times.Once);
        response.Should().NotBeNull();
    }
}

