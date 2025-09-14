using conduit.logging;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace conduit.tests;

public class SimpleTests
{
    private readonly IServiceProvider _provider;

    public SimpleTests()
    {
        var services = new ServiceCollection();
        services.AddConduit(c => c.RegisterHandlersAsImplementedFrom<SimpleTests>());
        _provider = services.BuildServiceProvider();
    }
    
    [Fact]
    public async Task ValidResultReturned()
    {
        // Act
        var conduit = _provider.GetRequiredService<IConduit>();
        var response = await conduit.PushAsync(new TestRequest(), CancellationToken.None);

        // Assert
        Assert.NotNull(response);
    }
}

public class TestRequest : IRequest<TestResponse>
{
    public string Message { get; set; } = string.Empty;
}

public class TestResponse
{
    public string Value { get; set; } = string.Empty;
}

public class TestHandler(ILog logger) : RequestHandler<TestRequest, TestResponse>(logger)
{
    public override Task<TestResponse> HandleAsync(TestRequest testRequest, CancellationToken cancellationToken = default)
    {
        Logger.Info(testRequest.Message);
        return Task.FromResult(new TestResponse() { Value = testRequest.Message });
    }
}