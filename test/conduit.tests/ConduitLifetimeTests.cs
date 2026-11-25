using conduit.logging;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace conduit.tests;

public class ConduitLifetimeTests
{
    public class ScopeMarker
    {
        public Guid Id { get; } = Guid.NewGuid();
    }

    public class ScopeProbeRequest : IRequest<ScopeProbeResponse>;

    public class ScopeProbeResponse
    {
        public Guid ScopeId { get; init; }
    }

    public class ScopeProbeHandler(ILog logger, ScopeMarker marker) : RequestHandler<ScopeProbeRequest, ScopeProbeResponse>(logger)
    {
        public override Task<ScopeProbeResponse> HandleAsync(ScopeProbeRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(new ScopeProbeResponse { ScopeId = marker.Id });
    }

    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddScoped<ScopeMarker>();
        services.AddConduit(
            c => c.RegisterHandler<ScopeProbeRequest, ScopeProbeResponse, ScopeProbeHandler>(),
            new Mock<ILog>().Object);
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    private static async Task<Guid> PushInScope(IServiceScope scope)
    {
        var conduit = scope.ServiceProvider.GetRequiredService<IConduit>();
        var response = await conduit.PushAsync(new ScopeProbeRequest());
        return response!.ScopeId;
    }

    [Fact]
    public async Task PushSucceeds_WhenProviderValidatesScopes()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        var act = () => PushInScope(scope);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task HandlerDependencies_AreResolvedFromTheCallersScope()
    {
        using var provider = BuildProvider();
        using var scopeA = provider.CreateScope();
        using var scopeB = provider.CreateScope();

        var a1 = await PushInScope(scopeA);
        var a2 = await PushInScope(scopeA);
        var b = await PushInScope(scopeB);

        a1.Should().Be(scopeA.ServiceProvider.GetRequiredService<ScopeMarker>().Id);
        a2.Should().Be(a1);
        b.Should().NotBe(a1);
    }
}
