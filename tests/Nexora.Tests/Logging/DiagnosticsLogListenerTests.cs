using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Nexora.Logging;
using Tests.Shared.Logging;

namespace Nexora.Tests.Logging;

public class DiagnosticsLogListenerTests
{
    private readonly TestLogger<DiagnosticsLogListener> logger = new TestLogger<DiagnosticsLogListener>();
    private readonly DiagnosticsLogListener listener;

    public DiagnosticsLogListenerTests()
    {
        listener = new DiagnosticsLogListener(logger);
    }

    [Theory]
    [InlineData(TraceEventType.Critical, LogLevel.Error)]
    [InlineData(TraceEventType.Error, LogLevel.Warning)]
    [InlineData(TraceEventType.Warning, LogLevel.Debug)]
    [InlineData(TraceEventType.Information, LogLevel.Trace)]
    public void TraceEvent_EventType_LogsWithMappedLevel(TraceEventType eventType, LogLevel expected)
    {
        listener.TraceEvent(null, "Nexora", eventType, 0, "Message");

        AssertLogged(expected, "(Diagnostics) Message");
    }

    [Fact]
    public void TraceEvent_FormatWithArguments_FormatsThem()
    {
        listener.TraceEvent(null, "Nexora", TraceEventType.Error, 0, "Error parsing path \"{0}\": {1}", "M0 0 X", "Unexpected token");

        AssertLogged(LogLevel.Warning, "(Diagnostics) Error parsing path \"M0 0 X\": Unexpected token");
    }

    [Fact]
    public void TraceEvent_FormatWithBracesAndNoArguments_KeepsItAsIs()
    {
        listener.TraceEvent(null, "Nexora", TraceEventType.Error, 0, "Value {not a placeholder}", []);

        AssertLogged(LogLevel.Warning, "(Diagnostics) Value {not a placeholder}");
    }

    [Fact]
    public void WriteLine_Called_LogsDebug()
    {
        listener.WriteLine("Message");

        AssertLogged(LogLevel.Debug, "(Diagnostics) Message");
    }

    private void AssertLogged(LogLevel level, string message)
    {
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(level, entry.Level);
        Assert.Equal(message, entry.Message);
    }
}
