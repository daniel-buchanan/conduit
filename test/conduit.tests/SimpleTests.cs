using conduit.logging;
using conduit.tests.Handlers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace conduit.tests;

public class SimpleTests
{
    private readonly IServiceProvider _provider;
    private readonly Mock<ILog> _loggerMock;

    public SimpleTests()
    {
        _loggerMock = new Mock<ILog>();
        var services = new ServiceCollection();
        services.AddConduit(c => c.RegisterHandlersAsImplementedFrom<SimpleTests>(), _loggerMock.Object);
        _provider = services.BuildServiceProvider();
    }
    
    [Fact]
    public async Task ValidResultReturned()
    {
        // Act
        var conduit = _provider.GetRequiredService<IConduit>();
        var response = await conduit.PushAsync(new TestRequest(), CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
    }
    
    [Fact]
    public async Task LoggerInfoCalled()
    {
        // Act
        var conduit = _provider.GetRequiredService<IConduit>();
        await conduit.PushAsync(new TestRequest(), CancellationToken.None);

        // Assert
        _loggerMock.Verify(l => l.Info(It.IsAny<string>()), Times.Once);
    }
}