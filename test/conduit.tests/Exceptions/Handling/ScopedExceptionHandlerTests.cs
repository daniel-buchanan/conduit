using conduit.Exceptions.Handling;
using conduit.logging;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace conduit.tests.Exceptions.Handling;

/// <summary>
/// Exception handlers are resolved from the scope <see cref="IConduit"/> was resolved in (ADR-0017), so a
/// <c>Scoped</c> registration behaves as a per-scope instance.
/// </summary>
public class ScopedExceptionHandlerTests
{
    public class ScopeMarker
    {
        public Guid Id { get; } = Guid.NewGuid();
    }

    public class ScopedFailureRequest : IRequest<ScopedFailureResponse>;

    public class ScopedFailureResponse
    {
        public Guid ScopeId { get; init; }
    }

    public class ThrowingHandler(ILog logger) : RequestHandler<ScopedFailureRequest, ScopedFailureResponse>(logger)
    {
        public override Task<ScopedFailureResponse> HandleAsync(ScopedFailureRequest request, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("boom");
    }

    public class ScopedRecoveringHandler(ScopeMarker marker)
        : IRequestExceptionHandler<ScopedFailureRequest, ScopedFailureResponse, InvalidOperationException>
    {
        public int Order => 0;

        public Task HandleAsync(
            ScopedFailureRequest request,
            InvalidOperationException exception,
            RequestExceptionHandlerState<ScopedFailureResponse> state,
            CancellationToken cancellationToken)
        {
            state.SetHandled(new ScopedFailureResponse { ScopeId = marker.Id });
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task A_Scoped_Exception_Handler_Is_Resolved_From_The_Callers_Scope()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<ScopeMarker>();
        services.AddConduit(
            c => c.RegisterHandler<ScopedFailureRequest, ScopedFailureResponse, ThrowingHandler>(),
            new Mock<ILog>().Object);
        services.AddScoped<
            IRequestExceptionHandler<ScopedFailureRequest, ScopedFailureResponse, InvalidOperationException>,
            ScopedRecoveringHandler>();
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using var scopeA = provider.CreateScope();
        using var scopeB = provider.CreateScope();

        // Act
        var a = await scopeA.ServiceProvider.GetRequiredService<IConduit>().PushAsync(new ScopedFailureRequest());
        var b = await scopeB.ServiceProvider.GetRequiredService<IConduit>().PushAsync(new ScopedFailureRequest());

        // Assert
        a!.ScopeId.Should().Be(scopeA.ServiceProvider.GetRequiredService<ScopeMarker>().Id);
        b!.ScopeId.Should().NotBe(a.ScopeId);
    }
}
