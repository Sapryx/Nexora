using System.Diagnostics;
using System.Globalization;
using Core.Logging;
using Microsoft.Extensions.Logging;

namespace Nexora.Logging;

public class DiagnosticsLogListener : TraceListener
{
    private readonly ILogger<DiagnosticsLogListener> logger;

    public DiagnosticsLogListener(ILogger<DiagnosticsLogListener> logger)
    {
        this.logger = logger;
    }

    public override void Write(string? message)
    {
        WriteLine(message);
    }

    public override void WriteLine(string? message)
    {
        logger.Debug($"(Diagnostics) {message}");
    }

    public override void TraceEvent(TraceEventCache? eventCache, string source, TraceEventType eventType, int id, string? format, params object?[]? args)
    {
        string? message = args is null || args.Length == 0
            ? format
            : string.Format(CultureInfo.InvariantCulture, format ?? "", args);

        TraceEvent(eventCache, source, eventType, id, message);
    }

    public override void TraceEvent(TraceEventCache? eventCache, string source, TraceEventType eventType, int id, string? message)
    {
        switch(eventType)
        {
            case TraceEventType.Critical:
                logger.Error($"(Diagnostics) {message}");
                break;
            case TraceEventType.Error:
                logger.Warn($"(Diagnostics) {message}");
                break;
            case TraceEventType.Warning:
                logger.Debug($"(Diagnostics) {message}");
                break;
            default:
                logger.Trace($"(Diagnostics) {message}");
                break;
        }
    }
}
