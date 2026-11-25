using conduit.common;
using FluentAssertions;
using Xunit;

namespace conduit.tests.Common;

/// <summary>
/// Mutates real process environment variables, which every <see cref="EnvironmentImpl"/> in the test run
/// reads (e.g. the default <c>ConduitConfigurationBuilder</c>), so this collection runs on its own.
/// </summary>
[CollectionDefinition(nameof(ProcessEnvironmentCollection), DisableParallelization = true)]
public class ProcessEnvironmentCollection;

[Collection(nameof(ProcessEnvironmentCollection))]
public class EnvironmentImplTests
{
    private static void WithVariable(string name, string value, Action act)
    {
        var original = Environment.GetEnvironmentVariable(name);
        Environment.SetEnvironmentVariable(name, value);
        try { act(); }
        finally { Environment.SetEnvironmentVariable(name, original); }
    }

    [Fact]
    public void LogLevel_ReadsTheLogLevelVariablesValue()
        => WithVariable(EnvironmentImpl.LogLevelVariable, nameof(LoggingLevel.Debug),
            () => new EnvironmentImpl().LogLevel.Should().Be(LoggingLevel.Debug));

    [Fact]
    public void LogLevel_FallsBackToInfo_WhenValueIsNotALevel()
        => WithVariable(EnvironmentImpl.LogLevelVariable, "not-a-level",
            () => new EnvironmentImpl().LogLevel.Should().Be(LoggingLevel.Info));

    [Fact]
    public void Environment_ReadsTheEnvironmentVariablesValue()
        => WithVariable(EnvironmentImpl.EnvironmentNameVariable, nameof(EnvironmentName.Production),
            () => new EnvironmentImpl().Environment.Should().Be(EnvironmentName.Production));

    [Fact]
    public void GetEnvironmentVariables_ReturnsProcessVariables()
        => WithVariable("CONDUIT_TEST_VARIABLE", "expected",
            () => new EnvironmentImpl().GetEnvironmentVariables()
                .Should().Contain("CONDUIT_TEST_VARIABLE", "expected"));
}
