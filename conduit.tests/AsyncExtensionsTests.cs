using conduit.common;
using Xunit;

namespace conduit.tests;

public class AsyncExtensionsTests
{
    [Fact]
    public void Await_Of_T_Should_Rethrow_The_Original_Exception_Not_An_AggregateException()
    {
        // Arrange: Task.Wait()/Task.Result wrap a faulted task's exception in AggregateException, hiding the
        // real exception type from any synchronous caller (e.g. ModelValidator.Validate/Rule.Validate).
        var task = Task.FromException<int>(new InvalidOperationException("boom"));

        // Act
        void Act() => task.Await();

        // Assert
        var exception = Assert.Throws<InvalidOperationException>(Act);
        Assert.Equal("boom", exception.Message);
    }

    [Fact]
    public void Await_Should_Rethrow_The_Original_Exception_Not_An_AggregateException()
    {
        // Arrange
        var task = Task.FromException(new InvalidOperationException("boom"));

        // Act
        void Act() => task.Await();

        // Assert
        var exception = Assert.Throws<InvalidOperationException>(Act);
        Assert.Equal("boom", exception.Message);
    }
}
