using conduit.logging;
using conduit.tests.Handlers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace conduit.tests;

public class ConduitPushTests
{
    private readonly IServiceProvider _provider;
    private readonly Mock<ILog> _loggerMock;

    public ConduitPushTests()
    {
        _loggerMock = new Mock<ILog>();
        var services = new ServiceCollection();
        services.AddConduit(c => c.RegisterHandlersAsImplementedFrom<ConduitPushTests>(), _loggerMock.Object);
        _provider = services.BuildServiceProvider();
    }
    
    [Fact]
    public async Task PushAsync_Returns_A_Response_From_A_Scanned_Handler()
    {
        // Act
        var conduit = _provider.GetRequiredService<IConduit>();
        var response = await conduit.PushAsync(new TestRequest(), CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
    }
    
    [Fact]
    public async Task PushAsync_Runs_The_Handler_Once()
    {
        // Act
        var conduit = _provider.GetRequiredService<IConduit>();
        await conduit.PushAsync(new TestRequest(), CancellationToken.None);

        // Assert
        _loggerMock.Verify(l => l.Info(It.IsAny<string>()), Times.Once);
    }
}