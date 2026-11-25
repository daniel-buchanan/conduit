using conduit.common;
using conduit.logging;
using FluentAssertions;
using Moq;
using Xunit;

namespace conduit.tests.Logging;

public class CoreLoggingTests
{
    private class CapturingLogger(IEnvironment environment) : Log(environment)
    {
        public List<(string Level, string Message)> Written { get; } = new();

        protected override ILog WriteMessageInternal(string level, string message)
        {
            Written.Add((level, message));
            return this;
        }
    }

    private static CapturingLogger CreateLogger(LoggingLevel level)
    {
        var envMock = new Mock<IEnvironment>();
        envMock.Setup(e => e.LogLevel).Returns(level);
        return new CapturingLogger(envMock.Object);
    }

    [Fact]
    public void MessageWritten_AtConfiguredLevel()
    {
        var logger = CreateLogger(LoggingLevel.Info);

        logger.Info("Test message");

        logger.Written.Should().ContainSingle().Which.Message.Should().Be("Test message");
    }

    [Theory]
    [MemberData(nameof(BelowConfiguredLevelData))]
    public void MessageNotWritten_BelowConfiguredLevel(LoggingLevel level, Func<ILog, ILog> logFunc)
    {
        var logger = CreateLogger(level);

        logFunc(logger);

        logger.Written.Should().BeEmpty();
    }

    public static TheoryData<LoggingLevel, Func<ILog, ILog>> BelowConfiguredLevelData => new()
    {
        { LoggingLevel.Verbose, l => l.Debug("Test message") },
        { LoggingLevel.Info, l => l.Verbose("Test message") },
        { LoggingLevel.Info, l => l.Debug("Test message") },
        { LoggingLevel.Warning, l => l.Info("Test message") },
        { LoggingLevel.Warning, l => l.Verbose("Test message") },
        { LoggingLevel.Warning, l => l.Debug("Test message") },
        { LoggingLevel.Error, l => l.Warn("Test message") },
        { LoggingLevel.Error, l => l.Info("Test message") },
        { LoggingLevel.Error, l => l.Verbose("Test message") },
        { LoggingLevel.Error, l => l.Debug("Test message") },
    };

    [Theory]
    [InlineData(LoggingLevel.Debug, "DEBUG")]
    [InlineData(LoggingLevel.Verbose, "VERB ")]
    [InlineData(LoggingLevel.Info, "INFO ")]
    [InlineData(LoggingLevel.Warning, "WARN ")]
    [InlineData(LoggingLevel.Error, "ERR  ")]
    public void LevelLabel_PaddedToFiveCharacters(LoggingLevel level, string expectedLabel)
    {
        var logger = CreateLogger(LoggingLevel.Debug);

        Func<string, ILog> log = level switch
        {
            LoggingLevel.Debug => logger.Debug,
            LoggingLevel.Verbose => logger.Verbose,
            LoggingLevel.Info => logger.Info,
            LoggingLevel.Warning => logger.Warn,
            _ => logger.Error,
        };
        log("Test message");

        logger.Written.Should().ContainSingle().Which.Level.Should().Be(expectedLabel);
    }

    [Theory]
    [MemberData(nameof(ExceptionWithMessageData))]
    public void ExceptionOverload_WritesMessageThenException(LoggingLevel level, Func<ILog, string, Exception, ILog> logFunc)
    {
        var logger = CreateLogger(level);

        logFunc(logger, "Context message", new Exception("Test exception"));

        var messages = logger.Written.Select(w => w.Message).ToArray();
        messages[0].Should().Be("Context message");
        messages[1].Should().Be("Error occurred! Test exception");
    }

    public static TheoryData<LoggingLevel, Func<ILog, string, Exception, ILog>> ExceptionWithMessageData => new()
    {
        { LoggingLevel.Debug, (l, s, e) => l.Debug(s, e) },
        { LoggingLevel.Verbose, (l, s, e) => l.Verbose(s, e) },
        { LoggingLevel.Info, (l, s, e) => l.Info(s, e) },
        { LoggingLevel.Warning, (l, s, e) => l.Warn(s, e) },
        { LoggingLevel.Error, (l, s, e) => l.Error(s, e) },
        { LoggingLevel.Error, (l, s, e) => l.Error(e, s) },
    };

    [Fact]
    public void ErrorWithOnlyException_WritesException()
    {
        var logger = CreateLogger(LoggingLevel.Error);

        logger.Error(new Exception("Test exception"));

        logger.Written.Should().ContainSingle().Which.Message.Should().Be("Error occurred! Test exception");
    }
}
