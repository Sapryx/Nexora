using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Nexora.Logging;

public static class LogLevelArgument
{
    public const string Name = "--log-level";

    private static readonly Dictionary<string, LogLevel> Levels = new Dictionary<string, LogLevel>(StringComparer.OrdinalIgnoreCase)
    {
        { "trace", LogLevel.Trace },
        { "debug", LogLevel.Debug },
        { "info", LogLevel.Information },
        { "warn", LogLevel.Warning },
        { "error", LogLevel.Error },
        { "crit", LogLevel.Critical }
    };

    public static string KnownNames => string.Join(", ", Levels.Keys);

    public static string? FindValue(string[] args)
    {
        int index = Array.IndexOf(args, Name);

        if(index < 0)
        {
            return null;
        }

        return index + 1 < args.Length ? args[index + 1] : "";
    }

    public static bool TryParse(string value, out LogLevel level)
    {
        return Levels.TryGetValue(value, out level);
    }
}
