using conduit.Exceptions.Handling;
using conduit.logging;
using conduit.Pipes;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace conduit.tests.Configuration;

public class RegisterExceptionHandlersFromAssemblyContainingTests
{
    public class ScannedRequest : IRequest<ScannedResponse>;

    public class ScannedResponse
    {
        public string Value { get; init; } = string.Empty;
    }

    public class ThrowingHandler(ILog logger) : RequestHandler<ScannedRequest, ScannedResponse>(logger)
    {
        public override Task<ScannedResponse> HandleAsync(ScannedRequest request, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("boom");
    }

    /// <summary>One class implementing both an action and a handler, so the scan must register it under each.</summary>
    public class ObservingRecoverer
        : IRequestExceptionAction<ScannedRequest, InvalidOperationException>,
          IRequestExceptionHandler<ScannedRequest, ScannedResponse, InvalidOperationException>
    {
        public static int ActionCount;
        public int Order => 0;

        public Task ExecuteAsync(ScannedRequest request, InvalidOperationException exception, CancellationToken cancellationToken)
        {
            ActionCount++;
            return Task.CompletedTask;
        }

        public Task HandleAsync(
            ScannedRequest request,
            InvalidOperationException exception,
            RequestExceptionHandlerState<ScannedResponse> state,
            CancellationToken cancellationToken)
        {
            state.SetHandled(new ScannedResponse { Value = "recovered-by-scan" });
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task Scanned_Exception_Handlers_And_Actions_Are_Registered_And_Run()
    {
        // Arrange
        ObservingRecoverer.ActionCount = 0;
        var services = new ServiceCollection();
        services.AddConduit(c =>
        {
            c.RegisterHandler<ScannedRequest, ScannedResponse, ThrowingHandler>();
            c.RegisterExceptionHandlersFromAssemblyContaining<RegisterExceptionHandlersFromAssemblyContainingTests>();
        }, new Mock<ILog>().Object);
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using var scope = provider.CreateScope();

        // Act
        var response = await scope.ServiceProvider.GetRequiredService<IConduit>().PushAsync(new ScannedRequest());

        // Assert
        response!.Value.Should().Be("recovered-by-scan");
        ObservingRecoverer.ActionCount.Should().Be(1);
    }

    [Fact]
    public void Scanned_Exception_Handlers_Are_Registered_As_Scoped()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddConduit(
            c => c.RegisterExceptionHandlersFromAssemblyContaining<RegisterExceptionHandlersFromAssemblyContainingTests>(),
            new Mock<ILog>().Object);

        // Assert
        services.Should().ContainSingle(d =>
                d.ServiceType == typeof(IRequestExceptionHandler<ScannedRequest, ScannedResponse, InvalidOperationException>))
            .Which.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }
}
