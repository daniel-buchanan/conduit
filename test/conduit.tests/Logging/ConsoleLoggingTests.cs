using conduit.common;
using conduit.logging;
using FluentAssertions;
using Moq;
using Xunit;

namespace conduit.tests.Logging;

public class ConsoleLoggingTests
{
    [Fact]
    public void WriteMessageInternalCalled()
    {
        // Arrange
        var env = new EnvironmentImpl();
        var stdOut = new MockStdIo(isStdErr: false);
        var stdErr = new MockStdIo(isStdErr: true);
        var console = new StdConsole(stdOut, stdErr);
        var logger = new ConsoleLog(env, console);
        var messageLogged = string.Empty;
        stdOut.OnWriteLine += (msg) => messageLogged = msg;

        // Act
        var testMessage = "Test message";
        logger.Info(testMessage);

        // Assert
        messageLogged.Should().EndWith(testMessage);
    }
    
    [Fact]
    public void MessageNotLogged_WrongLogLevel()
    {
        // Arrange
        var envMock = new Mock<IEnvironment>();
        envMock.Setup(e => e.LogLevel).Returns(LoggingLevel.Error);
        var stdOut = new MockStdIo(isStdErr: false);
        var stdErr = new MockStdIo(isStdErr: true);
        var console = new StdConsole(stdOut, stdErr);
        var logger = new ConsoleLog(envMock.Object, console);
        var messageLogged = string.Empty;
        stdOut.OnWriteLine += (msg) => messageLogged = msg;

        // Act
        logger.Info("Test message");

        // Assert
        Assert.Equal(string.Empty, messageLogged);
    }

    [Theory]
    [MemberData(nameof(LogLevelShouldNotLogData))]
    public void MessageLogged_CorrectLogLevel(LoggingLevel level, Func<ILog, ILog> logFunc)
    {
        // Arrange
        var envMock = new Mock<IEnvironment>();
        envMock.Setup(e => e.LogLevel).Returns(level);
        var stdOut = new MockStdIo(isStdErr: false);
        var stdErr = new MockStdIo(isStdErr: true);
        var console = new StdConsole(stdOut, stdErr);
        var logger = new ConsoleLog(envMock.Object, console);
        var messageLogged = string.Empty;
        stdOut.OnWriteLine += (msg) => messageLogged = msg;

        // Act
        logFunc(logger);

        // Assert
        Assert.Equal(string.Empty, messageLogged);
    }
    
    public static TheoryData<LoggingLevel, Func<ILog, ILog>> LogLevelShouldNotLogData => new TheoryData<LoggingLevel, Func<ILog, ILog>>
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
    [MemberData(nameof(ExceptionLoggedCorrectlyData))]
    public void ExceptionLoggedCorrectly(LoggingLevel level, Func<ILog, string, Exception, ILog> logFunc)
    {
        // Arrange
        var envMock = new Mock<IEnvironment>();
        envMock.Setup(e => e.LogLevel).Returns(level);
        var stdOut = new MockStdIo(isStdErr: false);
        var stdErr = new MockStdIo(isStdErr: true);
        var console = new StdConsole(stdOut, stdErr);
        var logger = new ConsoleLog(envMock.Object, console);
        const string exceptionMessage = "Test exception";
        var testException = new Exception(exceptionMessage);
        var messageLogged = string.Empty;
        stdOut.OnWriteLine += (msg) => messageLogged = msg;
        
        // Act
        logFunc(logger, exceptionMessage, testException);

        // Assert
        var failureMessage = $"Error occurred! {exceptionMessage}";
        messageLogged.Should().EndWith(failureMessage);
    }
    
    public static TheoryData<LoggingLevel, Func<ILog, string, Exception, ILog>> ExceptionLoggedCorrectlyData => new ()
    {
        { LoggingLevel.Debug, (l, s, e) => l.Debug(s, e) },
        { LoggingLevel.Verbose, (l, s, e) => l.Verbose(s, e) },
        { LoggingLevel.Info, (l, s, e) => l.Info(s, e) },
        { LoggingLevel.Warning, (l, s, e) => l.Warn(s, e) },
        { LoggingLevel.Error, (l, s, e) => l.Error(s, e) },
        { LoggingLevel.Error , (l, s, e) => l.Error(e) },
        { LoggingLevel.Error , (l, s, e) => l.Error(e, s) },
    };
}