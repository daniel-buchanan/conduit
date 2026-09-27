using conduit.Exceptions;
using conduit.logging;
using conduit.tests.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace conduit.tests;

public class RegisterHandlerDuplicateRegistrationTests
{
    [Fact]
    public void RegisterHandler_Should_Throw_When_The_Same_Request_Response_Pair_Is_Registered_Twice()
    {
        // Arrange
        var services = new ServiceCollection();
        void Configure(IConduitConfigurationBuilder c)
        {
            c.RegisterHandler<TestRequest, TestResponse, TestRequestHandler>();
            c.RegisterHandler<TestRequest, TestResponse, TestRequestHandler>();
        }

        // Act
        void Act() => services.AddConduit(Configure, new Mock<ILog>().Object);

        // Assert
        Assert.Throws<PipeAlreadyRegisteredException>(Act);
    }

    [Fact]
    public void RegisterHandler_Should_Throw_When_The_Pair_Was_Already_Registered_Via_RegisterPipe()
    {
        // Arrange: RegisterHandler and RegisterPipe now share one registry, so a pair registered through
        // one path must be rejected as a duplicate through the other.
        var services = new ServiceCollection();
        void Configure(IConduitConfigurationBuilder c)
        {
            c.RegisterPipe<TestRequest, TestResponse>(p => p.AddHandler<TestRequestHandler>());
            c.RegisterHandler<TestRequest, TestResponse, TestRequestHandler>();
        }

        // Act
        void Act() => services.AddConduit(Configure, new Mock<ILog>().Object);

        // Assert
        Assert.Throws<PipeAlreadyRegisteredException>(Act);
    }
}
