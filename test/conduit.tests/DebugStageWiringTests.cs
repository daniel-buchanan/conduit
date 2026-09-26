using conduit.common;
using conduit.Configuration;
using conduit.logging;
using conduit.Pipes;
using conduit.tests.Handlers;
using conduit.validation;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace conduit.tests;

public class DebugStageWiringTests
{
    // DebugPreExecutionStage/DebugPostExecutionStage only ever write a Logger.Debug(...) message, which the
    // logger itself already suppresses at any log level above Debug (see Log.WriteMessage). Wiring them into
    // every pipe's default stage list regardless of log level would just add pipe-execution overhead for a
    // stage that writes nothing, so ConduitConfigurationBuilder only wires them in when IEnvironment.LogLevel
    // is Debug.

    private static Mock<IEnvironment> MockEnvironment(LoggingLevel level)
    {
        var mock = new Mock<IEnvironment>();
        mock.Setup(e => e.LogLevel).Returns(level);
        return mock;
    }

    [Fact]
    public async Task RegisterHandler_Should_Include_Debug_Stages_When_LogLevel_Is_Debug()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddConduit(new Mock<ILog>().Object);
        var builder = new ConduitConfigurationBuilder(MockEnvironment(LoggingLevel.Debug).Object);
        builder.RegisterHandler<TestRequest, TestResponse, TestRequestHandler>();
        builder.Build(services);
        var provider = services.BuildServiceProvider();
        var pipe = provider.GetRequiredService<IPipe<TestRequest, TestResponse>>();

        // Act
        var result = await pipe.PushWithDebugAsync(new TestRequest { Message = "hello" });

        // Assert
        Assert.Contains(result.Metrics, m => m.Name.Contains("DebugPreExecutionStage"));
        Assert.Contains(result.Metrics, m => m.Name.Contains("DebugPostExecutionStage"));
    }

    [Fact]
    public async Task RegisterHandler_Should_Not_Include_Debug_Stages_When_LogLevel_Is_Not_Debug()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddConduit(new Mock<ILog>().Object);
        var builder = new ConduitConfigurationBuilder(MockEnvironment(LoggingLevel.Info).Object);
        builder.RegisterHandler<TestRequest, TestResponse, TestRequestHandler>();
        builder.Build(services);
        var provider = services.BuildServiceProvider();
        var pipe = provider.GetRequiredService<IPipe<TestRequest, TestResponse>>();

        // Act
        var result = await pipe.PushWithDebugAsync(new TestRequest { Message = "hello" });

        // Assert
        Assert.DoesNotContain(result.Metrics, m => m.Name.Contains("DebugPreExecutionStage"));
        Assert.DoesNotContain(result.Metrics, m => m.Name.Contains("DebugPostExecutionStage"));
    }

    public class DebugWiredRequest : IRequest<DebugWiredResponse>
    {
        public string? Message { get; set; }
    }

    public class DebugWiredResponse
    {
        public string Value { get; set; } = string.Empty;
    }

    public class DebugWiredRequestHandler(ILog logger) : RequestHandler<DebugWiredRequest, DebugWiredResponse>(logger)
    {
        public override Task<DebugWiredResponse> HandleAsync(DebugWiredRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(new DebugWiredResponse { Value = request.Message ?? string.Empty });
    }

    [Fact]
    public async Task RegisterPipe_Should_Include_Debug_Stages_When_LogLevel_Is_Debug()
    {
        // Arrange: RegisterPipe never registers a closed DebugPreExecutionStage/DebugPostExecutionStage for
        // its own (TRequest, TResponse) pair the way RegisterHandler's GetServiceDescriptorsForHandler does —
        // it only resolves stages via DI at runtime, so this proves the open-generic registration in
        // ConduitConfigurationBuilder's constructor is what makes debug stages resolvable here too.
        var services = new ServiceCollection();
        services.AddConduit(new Mock<ILog>().Object);
        var builder = new ConduitConfigurationBuilder(MockEnvironment(LoggingLevel.Debug).Object);
        builder.RegisterPipe<DebugWiredRequest, DebugWiredResponse>(p => p.AddHandler<DebugWiredRequestHandler>());
        builder.Build(services);
        var provider = services.BuildServiceProvider();
        var pipe = provider.GetRequiredService<IPipe<DebugWiredRequest, DebugWiredResponse>>();

        // Act
        var result = await pipe.PushWithDebugAsync(new DebugWiredRequest { Message = "hello" });

        // Assert
        Assert.Contains(result.Metrics, m => m.Name.Contains("DebugPreExecutionStage"));
        Assert.Contains(result.Metrics, m => m.Name.Contains("DebugPostExecutionStage"));
    }

    [Fact]
    public async Task RegisterHandler_With_ExcludeValidation_Should_Still_Include_Debug_Stages_When_LogLevel_Is_Debug()
    {
        // Arrange: ExcludeValidation only filters default stages that implement IValidationPipeStage —
        // it must never accidentally filter out the (unrelated) default debug stages too.
        var services = new ServiceCollection();
        services.AddConduit(new Mock<ILog>().Object);
        var builder = new ConduitConfigurationBuilder(MockEnvironment(LoggingLevel.Debug).Object);
        builder.AddValidation(_ => { });
        builder.RegisterHandler<TestRequest, TestResponse, TestRequestHandler>(o => o.ExcludeValidation());
        builder.Build(services);
        var provider = services.BuildServiceProvider();
        var pipe = provider.GetRequiredService<IPipe<TestRequest, TestResponse>>();

        // Act
        var result = await pipe.PushWithDebugAsync(new TestRequest { Message = "hello" });

        // Assert
        Assert.Contains(result.Metrics, m => m.Name.Contains("DebugPreExecutionStage"));
        Assert.Contains(result.Metrics, m => m.Name.Contains("DebugPostExecutionStage"));
        Assert.DoesNotContain(result.Metrics, m => m.Name.Contains("ValidationStage"));
    }
}
