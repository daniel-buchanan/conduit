using conduit.Exceptions.Handling;
using conduit.logging;
using conduit.Pipes;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace conduit.tests.ExceptionHandling;

public class ExceptionHierarchyMatchingTests
{
    public class HierarchyRequest : IRequest<HierarchyResponse>;

    public class HierarchyResponse
    {
        public string Value { get; set; } = string.Empty;
    }

    public class PaymentException(string message) : Exception(message);

    public class PaymentDeclinedException(string message) : PaymentException(message);

    public class ThrowingHandler(ILog logger) : RequestHandler<HierarchyRequest, HierarchyResponse>(logger)
    {
        public override Task<HierarchyResponse> HandleAsync(HierarchyRequest request, CancellationToken cancellationToken = default)
            => throw new PaymentDeclinedException("card declined");
    }

    public class BaseTypeHandler : IRequestExceptionHandler<HierarchyRequest, HierarchyResponse, PaymentException>
    {
        public static int InvocationCount;
        public int Order => 0; // lower Order, even though registered against the LESS specific base type

        public Task HandleAsync(
            HierarchyRequest request,
            PaymentException exception,
            RequestExceptionHandlerState<HierarchyResponse> state,
            CancellationToken cancellationToken)
        {
            InvocationCount++;
            state.SetHandled(new HierarchyResponse { Value = "handled-by-base" });
            return Task.CompletedTask;
        }
    }

    public class DerivedTypeHandler : IRequestExceptionHandler<HierarchyRequest, HierarchyResponse, PaymentDeclinedException>
    {
        public static int InvocationCount;
        public int Order => 10; // higher Order, even though registered against the MORE specific derived type

        public Task HandleAsync(
            HierarchyRequest request,
            PaymentDeclinedException exception,
            RequestExceptionHandlerState<HierarchyResponse> state,
            CancellationToken cancellationToken)
        {
            InvocationCount++;
            state.SetHandled(new HierarchyResponse { Value = "handled-by-derived" });
            return Task.CompletedTask;
        }
    }

    public class BaseTypeHandlerHighOrder : IRequestExceptionHandler<HierarchyRequest, HierarchyResponse, PaymentException>
    {
        public static int InvocationCount;
        public int Order => 10; // higher Order, registered against the LESS specific base type

        public Task HandleAsync(
            HierarchyRequest request,
            PaymentException exception,
            RequestExceptionHandlerState<HierarchyResponse> state,
            CancellationToken cancellationToken)
        {
            InvocationCount++;
            state.SetHandled(new HierarchyResponse { Value = "handled-by-base-high-order" });
            return Task.CompletedTask;
        }
    }

    public class DerivedTypeHandlerLowOrder : IRequestExceptionHandler<HierarchyRequest, HierarchyResponse, PaymentDeclinedException>
    {
        public static int InvocationCount;
        public int Order => 0; // lower Order, registered against the MORE specific derived type

        public Task HandleAsync(
            HierarchyRequest request,
            PaymentDeclinedException exception,
            RequestExceptionHandlerState<HierarchyResponse> state,
            CancellationToken cancellationToken)
        {
            InvocationCount++;
            state.SetHandled(new HierarchyResponse { Value = "handled-by-derived-low-order" });
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task A_Handler_Registered_For_A_Base_Exception_Type_Should_Catch_A_Derived_Exception()
    {
        // Arrange
        BaseTypeHandler.InvocationCount = 0;
        var services = new ServiceCollection();
        services.AddConduit(c => c.RegisterPipe<HierarchyRequest, HierarchyResponse>(p => p.AddHandler<ThrowingHandler>()), new Mock<ILog>().Object);
        services.AddTransient<IRequestExceptionHandler<HierarchyRequest, HierarchyResponse, PaymentException>, BaseTypeHandler>();
        var provider = services.BuildServiceProvider();
        var pipe = provider.GetRequiredService<IPipe<HierarchyRequest, HierarchyResponse>>();

        // Act
        var response = await pipe.PushAsync(new HierarchyRequest());

        // Assert
        Assert.Equal("handled-by-base", response!.Value);
    }

    [Fact]
    public async Task Order_Determines_Which_Handler_Wins_Not_Exception_Type_Specificity()
    {
        // Arrange: BaseTypeHandler (Order 0, registered for PaymentException) and DerivedTypeHandler
        // (Order 10, registered for the exact thrown type PaymentDeclinedException) both match. The lower
        // Order must win regardless of which one is the more specific registration.
        DerivedTypeHandler.InvocationCount = 0;
        var services = new ServiceCollection();
        services.AddConduit(c => c.RegisterPipe<HierarchyRequest, HierarchyResponse>(p => p.AddHandler<ThrowingHandler>()), new Mock<ILog>().Object);
        services.AddTransient<IRequestExceptionHandler<HierarchyRequest, HierarchyResponse, PaymentException>, BaseTypeHandler>();
        services.AddTransient<IRequestExceptionHandler<HierarchyRequest, HierarchyResponse, PaymentDeclinedException>, DerivedTypeHandler>();
        var provider = services.BuildServiceProvider();
        var pipe = provider.GetRequiredService<IPipe<HierarchyRequest, HierarchyResponse>>();

        // Act
        var response = await pipe.PushAsync(new HierarchyRequest());

        // Assert: base-type handler (Order 0) ran and won; derived-type handler (Order 10) never got the chance.
        Assert.Equal("handled-by-base", response!.Value);
        Assert.Equal(0, DerivedTypeHandler.InvocationCount);
    }

    [Fact]
    public async Task Order_Beats_Exception_Type_Specificity_Reversed_Polarity()
    {
        // Arrange: Reverse polarity from the previous test. DerivedTypeHandlerLowOrder (Order 0, registered
        // for the exact thrown type PaymentDeclinedException) and BaseTypeHandlerHighOrder (Order 10,
        // registered for the less-specific base type PaymentException) both match. The lower Order must win
        // even though it's registered against the more specific type, proving Order is the sole decider in
        // both directions.
        BaseTypeHandlerHighOrder.InvocationCount = 0;
        DerivedTypeHandlerLowOrder.InvocationCount = 0;
        var services = new ServiceCollection();
        services.AddConduit(c => c.RegisterPipe<HierarchyRequest, HierarchyResponse>(p => p.AddHandler<ThrowingHandler>()), new Mock<ILog>().Object);
        services.AddTransient<IRequestExceptionHandler<HierarchyRequest, HierarchyResponse, PaymentException>, BaseTypeHandlerHighOrder>();
        services.AddTransient<IRequestExceptionHandler<HierarchyRequest, HierarchyResponse, PaymentDeclinedException>, DerivedTypeHandlerLowOrder>();
        var provider = services.BuildServiceProvider();
        var pipe = provider.GetRequiredService<IPipe<HierarchyRequest, HierarchyResponse>>();

        // Act
        var response = await pipe.PushAsync(new HierarchyRequest());

        // Assert: derived-type handler (Order 0) ran and won; base-type handler (Order 10) never got the chance.
        Assert.Equal("handled-by-derived-low-order", response!.Value);
        Assert.Equal(0, BaseTypeHandlerHighOrder.InvocationCount);
    }
}
