using conduit.Exceptions;
using conduit.logging;
using conduit.Pipes.Stages;
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

    [Fact]
    public void RegisterHandler_Should_Throw_When_The_Pair_Was_Already_Registered_Via_An_Assembly_Scan()
    {
        // Arrange: the scan registers TestRequestHandler for (TestRequest, TestResponse), so it must
        // share the same duplicate guard as the explicit registration paths (Q4 in the edge-case review).
        var services = new ServiceCollection();
        void Configure(IConduitConfigurationBuilder c)
        {
            c.RegisterHandlersAsImplementedFrom<RegisterHandlerDuplicateRegistrationTests>();
            c.RegisterHandler<TestRequest, TestResponse, TestRequestHandler>();
        }

        // Act
        void Act() => services.AddConduit(Configure, new Mock<ILog>().Object);

        // Assert
        Assert.Throws<PipeAlreadyRegisteredException>(Act);
    }

    public class DirectRequest : IRequest<DirectResponse>;

    public class DirectResponse
    {
        public string Value { get; init; } = string.Empty;
    }

    // Implements IRequestHandler<,> directly rather than deriving from RequestHandler<,>, so the closed
    // request/response types are only discoverable from its interfaces, not its base class.
    public class DirectlyImplementedHandler : IRequestHandler<DirectRequest, DirectResponse>
    {
        public Task<DirectResponse> HandleAsync(DirectRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(new DirectResponse { Value = "direct" });

        public async Task<StageResult<DirectRequest, DirectResponse>> ExecuteAsync(
            Guid instanceId, DirectRequest request, CancellationToken cancellationToken = default)
            => StageResult.WithResult<DirectRequest, DirectResponse>(await HandleAsync(request, cancellationToken), GetType());
    }

    [Fact]
    public async Task RegisterHandlersAsImplementedFrom_Should_Register_A_Handler_That_Implements_The_Interface_Directly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddConduit(c => c.RegisterHandlersAsImplementedFrom<RegisterHandlerDuplicateRegistrationTests>(), new Mock<ILog>().Object);
        using var scope = services.BuildServiceProvider().CreateScope();
        var conduit = scope.ServiceProvider.GetRequiredService<IConduit>();

        // Act
        var response = await conduit.PushAsync(new DirectRequest());

        // Assert
        Assert.Equal("direct", response!.Value);
    }
}
