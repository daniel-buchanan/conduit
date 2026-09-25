using conduit.logging;
using FluentAssertions;
using Xunit;

namespace conduit.tests.Logging;

public class StdConsoleTests
{
    [Fact]
    public void StdOutLogs()
    {
        // Arrange
        var stdOut = new MockStdIo(isStdErr: false);
        var stdErr = new MockStdIo(isStdErr: true);
        var logger = new StdConsole(stdOut, stdErr);
        const string testMessage = "This is a test message.";
        string? loggedMessage = null;
        stdOut.OnWriteLine += msg => loggedMessage = msg;
        
        // Act
        logger.Out.WriteLine(testMessage);
        
        // Assert
        loggedMessage.Should().Be(testMessage);
    }
    
    [Theory]
    [InlineData("Hello, {0}!", new[] { "World" })]
    [InlineData("The answer is {0}.", new[] { "42" })]
    [InlineData("{0} + {0} = {1}", new object[] { 1, 2 })]
    public void StdOutLogsWithTemplate(string template, object[] parameters)
    {
        // Arrange
        var stdOut = new MockStdIo(isStdErr: false);
        var stdErr = new MockStdIo(isStdErr: true);
        var logger = new StdConsole(stdOut, stdErr);
        var testMessage = string.Format(template, parameters);
        string? loggedMessage = null;
        stdOut.OnWriteLine += msg => loggedMessage = msg;
        
        // Act
        logger.Out.WriteLine(template, parameters);
        
        // Assert
        loggedMessage.Should().Be(testMessage);
    }
    
    [Theory]
    [InlineData("Hello, {0}!", new[] { "World" })]
    [InlineData("The answer is {0}.", new[] { "42" })]
    [InlineData("{0} + {0} = {1}", new object[] { 1, 2 })]
    public void StdErrorLogsWithTemplate(string template, object[] parameters)
    {
        // Arrange
        var stdOut = new MockStdIo(isStdErr: false);
        var stdErr = new MockStdIo(isStdErr: true);
        var logger = new StdConsole(stdOut, stdErr);
        var testMessage = string.Format(template, parameters);
        string? loggedMessage = null;
        stdErr.OnWriteLine += msg => loggedMessage = msg;
        
        // Act
        logger.Error.WriteLine(template, parameters);
        
        // Assert
        loggedMessage.Should().Be(testMessage);
    }
    
    [Fact]
    public void StdErrLogs()
    {
        // Arrange
        var stdOut = new MockStdIo(isStdErr: false);
        var stdErr = new MockStdIo(isStdErr: true);
        var logger = new StdConsole(stdOut, stdErr);
        const string testMessage = "This is a test message.";
        string? loggedMessage = null;
        stdErr.OnWriteLine += msg => loggedMessage = msg;
        
        // Act
        logger.Error.WriteLine(testMessage);
        
        // Assert
        loggedMessage.Should().Be(testMessage);
    }
}