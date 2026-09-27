using conduit.Exceptions.Handling;
using conduit.logging;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace conduit.tests.ExceptionHandling;

/// <summary>
/// Every other exception-handling test resolves <c>IPipe&lt;,&gt;</c> directly after <c>RegisterPipe</c>.
/// This exercises the documented "simple path" instead — <c>RegisterHandler</c> plus <see cref="IConduit"/>
/// — to prove exception-handler dispatch also works through Conduit's normal entry point, not just the
/// lower-level pipe API.
/// </summary>
public class RegisterHandlerEntryPointTests
{
    public class SimplePathRequest : IRequest<SimplePathResponse>;

    public class SimplePathResponse
    {
        public string Value { get; set; } = string.Empty;
    }

    public class ThrowingHandler(ILog logger) : RequestHandler<SimplePathRequest, SimplePathResponse>(logger)
    {
        public override Task<SimplePathResponse> HandleAsync(SimplePathRequest request, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("handler boom via simple path");
    }

    public class RecoveringHandler : IRequestExceptionHandler<SimplePathRequest, SimplePathResponse, InvalidOperationException>
    {
        public int Order => 0;

        public Task HandleAsync(
            SimplePathRequest request,
            InvalidOperationException exception,
            RequestExceptionHandlerState<SimplePathResponse> state,
            CancellationToken cancellationToken)
        {
            state.SetHandled(new SimplePathResponse { Value = "recovered-via-conduit" });
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task A_Registered_Handler_Should_Recover_A_Thrown_Exception_Through_IConduit_PushAsync()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddConduit(c => c.RegisterHandler<SimplePathRequest, SimplePathResponse, ThrowingHandler>(), new Mock<ILog>().Object);
        services.AddTransient<IRequestExceptionHandler<SimplePathRequest, SimplePathResponse, InvalidOperationException>, RecoveringHandler>();
        var provider = services.BuildServiceProvider();
        var conduit = provider.GetRequiredService<IConduit>();

        // Act
        var response = await conduit.PushAsync(new SimplePathRequest());

        // Assert
        Assert.Equal("recovered-via-conduit", response!.Value);
    }
}
