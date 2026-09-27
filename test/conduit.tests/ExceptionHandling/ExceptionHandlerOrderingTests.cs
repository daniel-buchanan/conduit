using conduit.Exceptions.Handling;
using conduit.logging;
using conduit.Pipes;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace conduit.tests.ExceptionHandling;

public class ExceptionHandlerOrderingTests
{
    public class OrderingRequest : IRequest<OrderingResponse>;

    public class OrderingResponse
    {
        public string Value { get; set; } = string.Empty;
    }

    public class ThrowingHandler(ILog logger) : RequestHandler<OrderingRequest, OrderingResponse>(logger)
    {
        public override Task<OrderingResponse> HandleAsync(OrderingRequest request, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("boom");
    }

    public class FirstHandler : IRequestExceptionHandler<OrderingRequest, OrderingResponse, InvalidOperationException>
    {
        public int Order => 0;

        public Task HandleAsync(
            OrderingRequest request,
            InvalidOperationException exception,
            RequestExceptionHandlerState<OrderingResponse> state,
            CancellationToken cancellationToken)
        {
            state.SetHandled(new OrderingResponse { Value = "first" });
            return Task.CompletedTask;
        }
    }

    public class SecondHandler : IRequestExceptionHandler<OrderingRequest, OrderingResponse, InvalidOperationException>
    {
        public static int InvocationCount;
        public int Order => 10;

        public Task HandleAsync(
            OrderingRequest request,
            InvalidOperationException exception,
            RequestExceptionHandlerState<OrderingResponse> state,
            CancellationToken cancellationToken)
        {
            InvocationCount++;
            state.SetHandled(new OrderingResponse { Value = "second" });
            return Task.CompletedTask;
        }
    }

    public class FirstAction : IRequestExceptionAction<OrderingRequest, InvalidOperationException>
    {
        public static readonly List<string> InvocationOrder = [];
        public int Order => 10;

        public Task ExecuteAsync(OrderingRequest request, InvalidOperationException exception, CancellationToken cancellationToken)
        {
            InvocationOrder.Add(nameof(FirstAction));
            return Task.CompletedTask;
        }
    }

    public class SecondAction : IRequestExceptionAction<OrderingRequest, InvalidOperationException>
    {
        public int Order => 0;

        public Task ExecuteAsync(OrderingRequest request, InvalidOperationException exception, CancellationToken cancellationToken)
        {
            FirstAction.InvocationOrder.Add(nameof(SecondAction));
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task Lower_Order_Handler_Runs_First_And_Wins_Later_Handlers_Do_Not_Run()
    {
        // Arrange: SecondHandler (Order 10) is registered BEFORE FirstHandler (Order 0) — Order must
        // determine execution, not registration order. FirstHandler (Order 0) should still run first and win.
        SecondHandler.InvocationCount = 0;
        var services = new ServiceCollection();
        services.AddConduit(c => c.RegisterPipe<OrderingRequest, OrderingResponse>(p => p.AddHandler<ThrowingHandler>()), new Mock<ILog>().Object);
        services.AddTransient<IRequestExceptionHandler<OrderingRequest, OrderingResponse, InvalidOperationException>, SecondHandler>();
        services.AddTransient<IRequestExceptionHandler<OrderingRequest, OrderingResponse, InvalidOperationException>, FirstHandler>();
        var provider = services.BuildServiceProvider();
        var pipe = provider.GetRequiredService<IPipe<OrderingRequest, OrderingResponse>>();

        // Act
        var response = await pipe.PushAsync(new OrderingRequest());

        // Assert
        Assert.Equal("first", response!.Value);
        Assert.Equal(0, SecondHandler.InvocationCount);
    }

    [Fact]
    public async Task All_Actions_Run_In_Order_Regardless_Of_Registration_Order()
    {
        // Arrange: SecondAction (Order 0) is registered AFTER FirstAction (Order 10) — sorted execution
        // must still run SecondAction first, by Order, not by registration order.
        FirstAction.InvocationOrder.Clear();
        var services = new ServiceCollection();
        services.AddConduit(c => c.RegisterPipe<OrderingRequest, OrderingResponse>(p => p.AddHandler<ThrowingHandler>()), new Mock<ILog>().Object);
        services.AddTransient<IRequestExceptionAction<OrderingRequest, InvalidOperationException>, FirstAction>();
        services.AddTransient<IRequestExceptionAction<OrderingRequest, InvalidOperationException>, SecondAction>();
        var provider = services.BuildServiceProvider();
        var pipe = provider.GetRequiredService<IPipe<OrderingRequest, OrderingResponse>>();

        // Act
        Func<Task> act = () => pipe.PushAsync(new OrderingRequest());
        await Assert.ThrowsAsync<conduit.Exceptions.StageFailedException>(act);

        // Assert
        Assert.Equal([nameof(SecondAction), nameof(FirstAction)], FirstAction.InvocationOrder);
    }
}
