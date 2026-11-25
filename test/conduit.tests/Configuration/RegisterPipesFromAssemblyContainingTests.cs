using conduit.Helpers;
using conduit.logging;
using conduit.Pipes;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace conduit.tests.Configuration;

public class RegisterPipesFromAssemblyContainingTests
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
    // the scenario RegisterPipesFromAssemblyContaining<TLocator> exists for: discovering these by scanning an
    // assembly instead of registering each one by hand.
    public class ManualPipe(ILog logger, IServiceProvider provider) : Pipe<ManualPipeRequest, ManualPipeResponse>(logger, provider)
    {
        public override Task<ManualPipeResponse?> PushAsync(ManualPipeRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult<ManualPipeResponse?>(new ManualPipeResponse { Value = request.Message ?? string.Empty });

        public override Task<DebugResult<ManualPipeResponse?>> PushWithDebugAsync(ManualPipeRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(new DebugResult<ManualPipeResponse?>(new ManualPipeResponse(), 0, []));
    }

    [Fact]
    public async Task RegisterPipesFromAssemblyContaining_Should_Discover_And_Register_A_Manually_Implemented_Pipe()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddConduit(c => c.RegisterPipesFromAssemblyContaining<RegisterPipesFromAssemblyContainingTests>(), new Mock<ILog>().Object);
        var provider = services.BuildServiceProvider();
        var pipe = provider.GetRequiredService<IPipe<ManualPipeRequest, ManualPipeResponse>>();

        // Act
        var response = await pipe.PushAsync(new ManualPipeRequest { Message = "hello" });

        // Assert
        Assert.IsType<ManualPipe>(pipe);
        Assert.Equal("hello", response!.Value);
    }

    public class DirectPipeRequest : IRequest<DirectPipeResponse>;

    public class DirectPipeResponse;

    // Implements IPipe<,> directly with no generic base class, so its request/response types can only be
    // recovered from its closed generic interface — and it also implements the non-generic IPipe, which
    // must never be chosen as the registration's service type.
    public class DirectlyImplementedPipe : IPipe<DirectPipeRequest, DirectPipeResponse>
    {
        public Task<DirectPipeResponse?> PushAsync(DirectPipeRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult<DirectPipeResponse?>(new DirectPipeResponse());

        public Task<DebugResult<DirectPipeResponse?>> PushWithDebugAsync(DirectPipeRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(new DebugResult<DirectPipeResponse?>(new DirectPipeResponse(), 0, []));
    }

    [Fact]
    public void RegisterPipesFromAssemblyContaining_Should_Register_A_Pipe_That_Implements_The_Interface_Directly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddConduit(c => c.RegisterPipesFromAssemblyContaining<RegisterPipesFromAssemblyContainingTests>(), new Mock<ILog>().Object);
        using var scope = services.BuildServiceProvider().CreateScope();

        // Act
        var pipe = scope.ServiceProvider.GetService<IPipe<DirectPipeRequest, DirectPipeResponse>>();

        // Assert
        Assert.IsType<DirectlyImplementedPipe>(pipe);
    }

    // A dedicated marker (rather than the real IPipe) so this scan can't also pick up ManualPipe above:
    // GetTypesFromAssembly scans the whole assembly for TBase, not just this class's nested types.
    public interface IUnresolvableScanRoot;

    // A type implementing IUnresolvableScanRoot directly, with no generic base class to recover closed
    // generic type arguments from — the case GetRegistrationsFromAssemblyContaining can never resolve.
    public class UnresolvableInvalidPipe : IUnresolvableScanRoot;

    [Fact]
    public void GetRegistrationsFromAssemblyContaining_Should_Throw_When_A_Discovered_Types_Generic_Arguments_Cannot_Be_Determined()
    {
        // Act: previously this silently registered the type under itself instead of failing loud,
        // producing a pipe that could never be resolved via IPipe<TRequest,TResponse> (Q7/Q13 in the
        // edge-case review).
        void Act() => ReflectionHelper.GetRegistrationsFromAssemblyContaining<RegisterPipesFromAssemblyContainingTests, IUnresolvableScanRoot>();

        // Assert
        var exception = Assert.Throws<InvalidOperationException>(Act);
        Assert.Contains(nameof(UnresolvableInvalidPipe), exception.Message);
    }
}
