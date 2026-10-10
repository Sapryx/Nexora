using Microsoft.Extensions.Logging;

namespace Tests.Shared.Logging;

public class LogEntry
{
    public LogLevel Level { get; }
    public string Message { get; }
    public Exception? Exception { get; }

    public LogEntry(LogLevel level, string message, Exception? exception)
    {
        Level = level;
        Message = message;
        Exception = exception;
    }
}
