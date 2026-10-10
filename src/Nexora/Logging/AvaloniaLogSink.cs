using System.Text;
using Avalonia.Logging;
using Core.Logging;
using Microsoft.Extensions.Logging;

namespace Nexora.Logging;

public class AvaloniaLogSink : ILogSink
{
    private readonly ILogger logger;

    public AvaloniaLogSink(ILogger<AvaloniaLogSink> logger)
    {
        this.logger = logger;
    }

    public bool IsEnabled(LogEventLevel level, string area)
    {
        return level >= LogEventLevel.Warning;
    }

    public void Log(LogEventLevel level, string area, object? source, string messageTemplate)
    {
        Log(level, area, source, messageTemplate, []);
    }

    public void Log(LogEventLevel level, string area, object? source, string messageTemplate, params object?[] propertyValues)
    {
        if(!IsEnabled(level, area))
        {
            return;
        }

        string message = FormatMessage(messageTemplate, propertyValues);
        string sourceName = source is null ? "" : $" ({source.GetType().Name})";

        if(level == LogEventLevel.Warning)
        {
            logger.Warn($"(Avalonia) {area}: {message}{sourceName}");
        }
        else
        {
            logger.Error($"(Avalonia) {area}: {message}{sourceName}");
        }
    }

    private static string FormatMessage(string messageTemplate, object?[] propertyValues)
    {
        var message = new StringBuilder();
        int position = 0;
        int valueIndex = 0;

        while(position < messageTemplate.Length)
        {
            int start = messageTemplate.IndexOf('{', position);
            int end = start < 0 ? -1 : messageTemplate.IndexOf('}', start);

            if(end < 0 || valueIndex >= propertyValues.Length)
            {
                message.Append(messageTemplate, position, messageTemplate.Length - position);
                break;
            }

            message.Append(messageTemplate, position, start - position);
            message.Append(propertyValues[valueIndex++]);
            position = end + 1;
        }

        return message.Replace("\0", "").ToString();
    }
}
