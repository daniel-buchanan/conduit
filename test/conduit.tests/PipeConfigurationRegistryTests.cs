using conduit.common;
using conduit.Configuration;
using conduit.Pipes;
using conduit.tests.Handlers;
using Xunit;

namespace conduit.tests;

public class PipeConfigurationRegistryTests
{
    [Fact]
    public void Add_Should_Throw_When_The_Registry_Is_Locked()
    {
        // Arrange
        var registry = new PipeConfigurationRegistry(HashUtil.Instance);
        registry.Lock();

        // Act
        void Act() => registry.Add<TestRequest, TestResponse>(new PipeDescriptor<TestRequest, TestResponse>());

        // Assert: previously this silently no-opped instead of throwing (Q8 in the edge-case review).
        Assert.Throws<InvalidOperationException>(Act);
    }

    [Fact]
    public void Add_Should_Succeed_Before_The_Registry_Is_Locked()
    {
        // Arrange
        var registry = new PipeConfigurationRegistry(HashUtil.Instance);

        // Act
        registry.Add<TestRequest, TestResponse>(new PipeDescriptor<TestRequest, TestResponse>());

        // Assert
        Assert.NotNull(registry.Get<TestRequest, TestResponse>());
    }
}
