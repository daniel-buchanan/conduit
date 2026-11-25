using conduit.logging;
using conduit.Pipes.Stages;
using conduit.tests.Handlers;
using Moq;
using Xunit;

namespace conduit.tests.Pipes;

public class DebugStageTests
{
    [Fact]
    public async Task PreExecutionStage_Logs_The_Request_Entering_Its_Pipe()
    {
        var logger = new Mock<ILog>();
        var stage = new DebugPreExecutionStage<TestRequest, TestResponse>(logger.Object);

        await stage.ExecuteAsync(Guid.Empty, new TestRequest());

        logger.Verify(l => l.Debug("[PRE] {0} :: entering pipe", nameof(TestRequest)), Times.Once);
    }

    [Fact]
    public async Task PostExecutionStage_Logs_The_Request_Leaving_Its_Pipe()
    {
        var logger = new Mock<ILog>();
        var stage = new DebugPostExecutionStage<TestRequest, TestResponse>(logger.Object);

        await stage.ExecuteAsync(Guid.Empty, new TestRequest());

        logger.Verify(l => l.Debug("[POST] {0} :: leaving pipe", nameof(TestRequest)), Times.Once);
    }
}
