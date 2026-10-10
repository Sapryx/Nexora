using System;
using System.IO;
using System.Linq;
using Core.Logging;
using Microsoft.Extensions.Logging;

namespace Nexora.Logging;

public static class SessionLogFiles
{
    private const string SearchPattern = "session-*.log";

    public static string GetPath(string directory, DateTime startTime)
    {
        return Path.Combine(directory, $"session-{startTime:yyyy-MM-dd_HH-mm-ss}.log");
    }

    public static void DeleteOld(string currentLogPath, int keptCount, ILogger logger)
    {
        string directory = Path.GetDirectoryName(currentLogPath) ?? "";
        string currentLogName = Path.GetFileName(currentLogPath);

        var oldLogs = Directory.EnumerateFiles(directory, SearchPattern)
            .Where(it => Path.GetFileName(it) != currentLogName)
            .OrderDescending(StringComparer.Ordinal)
            .Skip(keptCount - 1);

        foreach(string path in oldLogs)
        {
            try
            {
                File.Delete(path);
            }
            catch(Exception ex) when(ex is IOException or UnauthorizedAccessException)
            {
                logger.Warn(ex, $"Failed to delete old session log {path}");
            }
        }
    }
}
