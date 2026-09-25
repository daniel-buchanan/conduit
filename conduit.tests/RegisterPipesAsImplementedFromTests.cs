using conduit.Helpers;
using conduit.logging;
using conduit.Pipes;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace conduit.tests;

public class RegisterPipesAsImplementedFromTests
{
    public class ManualPipeRequest : IRequest<ManualPipeResponse>
    {
        public string? Message { get; set; }
    }

    public class ManualPipeResponse
    {
        public string Value { get; set; } = string.Empty;
    }

    // A fully hand-written IPipe implementation (rather than one built via RegisterHandler/RegisterPipe),
    // the scenario RegisterPipesAsImplementedFrom<TLocator> exists for: discovering these by scanning an
    // assembly instead of registering each one by hand.
    public class ManualPipe(ILog logger, IServiceProvider provider) : Pipe<ManualPipeRequest, ManualPipeResponse>(logger, provider)
    {
        public override Task<ManualPipeResponse?> PushAsync(ManualPipeRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult<ManualPipeResponse?>(new ManualPipeResponse { Value = request.Message ?? string.Empty });

        public override Task<DebugResult<ManualPipeResponse?>> PushWithDebugAsync(ManualPipeRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(new DebugResult<ManualPipeResponse?>(new ManualPipeResponse(), 0, []));
    }

    [Fact]
    public async Task RegisterPipesAsImplementedFrom_Should_Discover_And_Register_A_Manually_Implemented_Pipe()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddConduit(c => c.RegisterPipesAsImplementedFrom<RegisterPipesAsImplementedFromTests>(), new Mock<ILog>().Object);
        var provider = services.BuildServiceProvider();
        var pipe = provider.GetRequiredService<IPipe<ManualPipeRequest, ManualPipeResponse>>();

        // Act
        var response = await pipe.PushAsync(new ManualPipeRequest { Message = "hello" });

        // Assert
        Assert.IsType<ManualPipe>(pipe);
        Assert.Equal("hello", response!.Value);
    }

    // A dedicated marker (rather than the real IPipe) so this scan can't also pick up ManualPipe above:
    // GetTypesFromAssembly scans the whole assembly for TBase, not just this class's nested types.
    public interface IUnresolvableScanRoot;

    // A type implementing IUnresolvableScanRoot directly, with no generic base class to recover closed
    // generic type arguments from — the case GetRegistrationsAsImplementedFrom can never resolve.
    public class UnresolvableInvalidPipe : IUnresolvableScanRoot;

    [Fact]
    public void GetRegistrationsAsImplementedFrom_Should_Throw_When_A_Discovered_Types_Generic_Arguments_Cannot_Be_Determined()
    {
        // Act: previously this silently registered the type under itself instead of failing loud,
        // producing a pipe that could never be resolved via IPipe<TRequest,TResponse> (Q7/Q13 in the
        // edge-case review).
        void Act() => ReflectionHelper.GetRegistrationsAsImplementedFrom<RegisterPipesAsImplementedFromTests, IUnresolvableScanRoot>();

        // Assert
        var exception = Assert.Throws<InvalidOperationException>(Act);
        Assert.Contains(nameof(UnresolvableInvalidPipe), exception.Message);
    }
}
