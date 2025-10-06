using conduit.logging;

namespace conduit.tests.Handlers;

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