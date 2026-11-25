using System.Globalization;
using conduit.common;
using conduit.logging;
using FluentAssertions;
using Moq;
using Xunit;

namespace conduit.tests.Logging;

/// <summary>
/// Console-specific behaviour only; level filtering and message composition live in <see cref="CoreLoggingTests"/>.
/// </summary>
public class ConsoleLoggingTests
{
    [Fact]
    public void MessageWrittenToStdOut()
    {
        var envMock = new Mock<IEnvironment>();
        envMock.Setup(e => e.LogLevel).Returns(LoggingLevel.Info);
        var stdOut = new MockStdIo(isStdErr: false);
        var logger = new ConsoleLog(envMock.Object, new StdConsole(stdOut, new MockStdIo(isStdErr: true)));
        var messageLogged = string.Empty;
        stdOut.OnWriteLine += msg => messageLogged = msg;

        logger.Info("Test message");

        messageLogged.Should().StartWith("[INFO ] ").And.EndWith(" Test message");
    }

    [Fact]
    public void Timestamp_Uses24HourClock()
    {
        var afternoon = new DateTimeOffset(2026, 1, 2, 13, 4, 5, TimeSpan.Zero);

        var line = string.Format(CultureInfo.InvariantCulture, ConsoleLog.LogFormat, "INFO ", afternoon, "m");

        line.Should().Be("[INFO ] 2026-01-02T13:04:05 m");
    }
}
