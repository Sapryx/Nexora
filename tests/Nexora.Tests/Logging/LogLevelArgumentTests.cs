using Microsoft.Extensions.Logging;
using Nexora.Logging;

namespace Nexora.Tests.Logging;

public class LogLevelArgumentTests
{
    [Fact]
    public void FindValue_ArgumentWithValue_ReturnsValue()
    {
        Assert.Equal("debug", LogLevelArgument.FindValue(["--other", "--log-level", "debug"]));
    }

    [Fact]
    public void FindValue_NoArgument_ReturnsNull()
    {
        Assert.Null(LogLevelArgument.FindValue(["--other", "debug"]));
    }

    [Fact]
    public void FindValue_ArgumentWithoutValue_ReturnsEmpty()
    {
        Assert.Equal("", LogLevelArgument.FindValue(["--log-level"]));
    }

    [Theory]
    [InlineData("trace", LogLevel.Trace)]
    [InlineData("debug", LogLevel.Debug)]
    [InlineData("info", LogLevel.Information)]
    [InlineData("warn", LogLevel.Warning)]
    [InlineData("error", LogLevel.Error)]
    [InlineData("crit", LogLevel.Critical)]
    [InlineData("DEBUG", LogLevel.Debug)]
    public void TryParse_KnownName_ReturnsLevel(string value, LogLevel expected)
    {
        Assert.True(LogLevelArgument.TryParse(value, out var level));
        Assert.Equal(expected, level);
    }

    [Theory]
    [InlineData("verbose")]
    [InlineData("information")]
    [InlineData("")]
    public void TryParse_UnknownName_ReturnsFalse(string value)
    {
        Assert.False(LogLevelArgument.TryParse(value, out _));
    }
}
