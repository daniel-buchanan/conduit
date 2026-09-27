using conduit.Exceptions.Handling;
using conduit.logging;
using conduit.Pipes;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace conduit.tests.ExceptionHandling;

public class RequestScopedActionTests
{
    public class ActionRequest : IRequest<ActionResponse>;

    public class ActionResponse;

    public class ThrowingHandler(ILog logger) : RequestHandler<ActionRequest, ActionResponse>(logger)
    {
        public override Task<ActionResponse> HandleAsync(ActionRequest request, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("handler boom");
    }

    public class ObservingAction : IRequestExceptionAction<ActionRequest, InvalidOperationException>
    {
        public static int InvocationCount;
        public int Order => 0;

        public Task ExecuteAsync(ActionRequest request, InvalidOperationException exception, CancellationToken cancellationToken)
        {
            InvocationCount++;
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task An_Action_Should_Run_But_The_Exception_Should_Still_Propagate_Wrapped()
    {
        // Arrange
        ObservingAction.InvocationCount = 0;
        var services = new ServiceCollection();
        services.AddConduit(c => c.RegisterPipe<ActionRequest, ActionResponse>(p => p.AddHandler<ThrowingHandler>()), new Mock<ILog>().Object);
        services.AddTransient<IRequestExceptionAction<ActionRequest, InvalidOperationException>, ObservingAction>();
        var provider = services.BuildServiceProvider();
        var pipe = provider.GetRequiredService<IPipe<ActionRequest, ActionResponse>>();

        // Act
        Func<Task> act = () => pipe.PushAsync(new ActionRequest());

        // Assert: the action ran (observed the exception), but since no handler ever calls SetHandled, the
        // pipe still falls through to today's wrap-and-throw behavior.
        var exception = await Assert.ThrowsAsync<conduit.Exceptions.StageFailedException>(act);
        Assert.IsType<InvalidOperationException>(exception.InnerException);
        Assert.Equal(1, ObservingAction.InvocationCount);
    }
}
