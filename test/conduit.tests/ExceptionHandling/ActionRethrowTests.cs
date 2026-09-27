using conduit.Exceptions.Handling;
using conduit.logging;
using conduit.Pipes;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace conduit.tests.ExceptionHandling;

/// <summary>
/// Mirrors <see cref="ExceptionHandlerRethrowTests"/>, but for an Action rather than a Handler — exercising
/// the other call site of <c>ExceptionHandlerDispatcher.InvokeAsTask</c> (the actions loop, not the
/// handlers loop).
/// </summary>
public class ActionRethrowTests
{
    public class ActionRethrowRequest : IRequest<ActionRethrowResponse>;

    public class ActionRethrowResponse;

    public class ThrowingHandler(ILog logger) : RequestHandler<ActionRethrowRequest, ActionRethrowResponse>(logger)
    {
        public override Task<ActionRethrowResponse> HandleAsync(ActionRethrowRequest request, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("original failure");
    }

    public class BrokenExceptionAction : IRequestExceptionAction<ActionRethrowRequest, InvalidOperationException>
    {
        public int Order => 0;

        public Task ExecuteAsync(ActionRethrowRequest request, InvalidOperationException exception, CancellationToken cancellationToken)
            => throw new NotSupportedException("action itself is broken");
    }

    [Fact]
    public async Task An_Action_That_Throws_Should_Propagate_Directly_Not_Be_Wrapped()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddConduit(c => c.RegisterPipe<ActionRethrowRequest, ActionRethrowResponse>(p => p.AddHandler<ThrowingHandler>()), new Mock<ILog>().Object);
        services.AddTransient<IRequestExceptionAction<ActionRethrowRequest, InvalidOperationException>, BrokenExceptionAction>();
        var provider = services.BuildServiceProvider();
        var pipe = provider.GetRequiredService<IPipe<ActionRethrowRequest, ActionRethrowResponse>>();

        // Act
        Func<Task> act = () => pipe.PushAsync(new ActionRethrowRequest());

        // Assert: the action's own exception surfaces raw — not StageFailedException, not swallowed.
        await Assert.ThrowsAsync<NotSupportedException>(act);
    }
}
