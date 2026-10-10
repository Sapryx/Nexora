using System.Text;
using Avalonia.Logging;
using Microsoft.Extensions.Logging;
using Nexora.Logging;
using Tests.Shared.Logging;

namespace Nexora.Tests.Logging;

public class AvaloniaLogSinkTests
{
    private readonly TestLogger<AvaloniaLogSink> logger = new TestLogger<AvaloniaLogSink>();
    private readonly AvaloniaLogSink sink;

    public AvaloniaLogSinkTests()
    {
        sink = new AvaloniaLogSink(logger);
    }

    [Theory]
    [InlineData(LogEventLevel.Verbose, false)]
    [InlineData(LogEventLevel.Debug, false)]
    [InlineData(LogEventLevel.Information, false)]
    [InlineData(LogEventLevel.Warning, true)]
    [InlineData(LogEventLevel.Error, true)]
    [InlineData(LogEventLevel.Fatal, true)]
    public void IsEnabled_Level_EnablesOnlyWarningsAndAbove(LogEventLevel level, bool expected)
    {
        Assert.Equal(expected, sink.IsEnabled(level, LogArea.Binding));
    }

    [Theory]
    [InlineData(LogEventLevel.Warning, LogLevel.Warning)]
    [InlineData(LogEventLevel.Error, LogLevel.Error)]
    [InlineData(LogEventLevel.Fatal, LogLevel.Error)]
    public void Log_EnabledLevel_LogsWithMatchingLevel(LogEventLevel level, LogLevel expected)
    {
        sink.Log(level, LogArea.Layout, null, "Message");
        Assert.Equal(expected, Assert.Single(logger.Entries).Level);
    }

    [Fact]
    public void Log_InformationLevel_DoesNotLog()
    {
        sink.Log(LogEventLevel.Information, LogArea.Layout, null, "Message");
        Assert.Empty(logger.Entries);
    }

    [Fact]
    public void Log_TemplateWithValues_SubstitutesThemAndAddsAvaloniaPrefixWithArea()
    {
        sink.Log(
            LogEventLevel.Warning,
            LogArea.Binding,
            null,
            "Error in binding to {Target}.{Property}: {Message}", "TextBlock", "Text", "Null value"
        );
        Assert.Equal("(Avalonia) Binding: Error in binding to TextBlock.Text: Null value", Assert.Single(logger.Entries).Message);
    }

    [Fact]
    public void Log_FewerValuesThanPlaceholders_KeepsRestOfTemplate()
    {
        sink.Log(LogEventLevel.Warning, LogArea.Binding, null, "Value {First} and {Second}", "one");
        Assert.Equal("(Avalonia) Binding: Value one and {Second}", Assert.Single(logger.Entries).Message);
    }

    [Fact]
    public void Log_ValueWithTrailingNullCharacters_RemovesThem()
    {
        sink.Log(LogEventLevel.Warning, LogArea.X11Platform, null, "SMLib reported an error: {Message}", "SESSION_MANAGER not defined\0\0\0");
        Assert.Equal("(Avalonia) X11Platform: SMLib reported an error: SESSION_MANAGER not defined", Assert.Single(logger.Entries).Message);
    }

    [Fact]
    public void Log_WithSource_AppendsSourceTypeName()
    {
        sink.Log(LogEventLevel.Warning, LogArea.Control, new StringBuilder(), "Message");
        Assert.Equal("(Avalonia) Control: Message (StringBuilder)", Assert.Single(logger.Entries).Message);
    }
}
