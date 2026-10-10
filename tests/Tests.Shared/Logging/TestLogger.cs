using Microsoft.Extensions.Logging;

namespace Tests.Shared.Logging;

public class TestLogger<T> : ILogger<T>
{
    public List<LogEntry> Entries { get; } = [];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        lock(Entries)
        {
            Entries.Add(new LogEntry(logLevel, formatter(state, exception), exception));
        }
    }
}
