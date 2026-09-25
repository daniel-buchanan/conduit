using conduit.logging;
using conduit.tests.Handlers;
using conduit.validation;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace conduit.tests.Validation;

public class SilentValidationTests
{
    [Fact]
    public async Task ValidationStage_Should_Succeed_Silently_When_No_Validator_Is_Registered_And_Not_Configured_To_Throw()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddConduit(c =>
        {
            c.AddValidation(_ => { });
            c.RegisterHandler<TestRequest, TestResponse, TestRequestHandler>();
        }, new Mock<ILog>().Object);
        var provider = services.BuildServiceProvider();
        var conduit = provider.GetRequiredService<IConduit>();

        // Act
        var response = await conduit.PushAsync(new TestRequest { Message = "hello" });

        // Assert
        Assert.NotNull(response);
        Assert.Equal("hello", response!.Value);
    }
}
