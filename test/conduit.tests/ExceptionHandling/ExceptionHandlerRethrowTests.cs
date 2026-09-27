using conduit.Exceptions.Handling;
using conduit.logging;
using conduit.Pipes;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace conduit.tests.ExceptionHandling;

public class ExceptionHandlerRethrowTests
{
    public class RethrowRequest : IRequest<RethrowResponse>;

    public class RethrowResponse;

    public class ThrowingHandler(ILog logger) : RequestHandler<RethrowRequest, RethrowResponse>(logger)
    {
        public override Task<RethrowResponse> HandleAsync(RethrowRequest request, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("original failure");
    }

    public class BrokenExceptionHandler : IRequestExceptionHandler<RethrowRequest, RethrowResponse, InvalidOperationException>
    {
        public int Order => 0;

        public Task HandleAsync(
            RethrowRequest request,
            InvalidOperationException exception,
            RequestExceptionHandlerState<RethrowResponse> state,
            CancellationToken cancellationToken)
            => throw new NotSupportedException("handler itself is broken");
    }

    [Fact]
    public async Task A_Handler_That_Throws_Should_Propagate_Directly_Not_Be_Wrapped()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddConduit(c => c.RegisterPipe<RethrowRequest, RethrowResponse>(p => p.AddHandler<ThrowingHandler>()), new Mock<ILog>().Object);
        services.AddTransient<IRequestExceptionHandler<RethrowRequest, RethrowResponse, InvalidOperationException>, BrokenExceptionHandler>();
        var provider = services.BuildServiceProvider();
        var pipe = provider.GetRequiredService<IPipe<RethrowRequest, RethrowResponse>>();

        // Act
        Func<Task> act = () => pipe.PushAsync(new RethrowRequest());

        // Assert: the handler's own exception surfaces raw — not StageFailedException, not swallowed.
        await Assert.ThrowsAsync<NotSupportedException>(act);
    }
}
