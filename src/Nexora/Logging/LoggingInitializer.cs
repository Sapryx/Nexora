using System;
using System.IO;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace Nexora.Logging;

public static class LoggingInitializer
{
    private const int KeptSessionLogCount = 10;
    private static readonly string LogsDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.DoNotVerify), "Nexora/logs");

    public static ILoggerFactory Initialize()
    {
        Directory.CreateDirectory(LogsDirectory);

        string sessionLogPath = SessionLogFiles.GetPath(LogsDirectory, DateTime.Now);
        var loggerFactory = CreateLoggerFactory(sessionLogPath);

        SessionLogFiles.DeleteOld(sessionLogPath, KeptSessionLogCount, loggerFactory.CreateLogger(typeof(SessionLogFiles)));

        return loggerFactory;
    }

    public static ILoggerFactory CreateLoggerFactory(string sessionLogPath)
    {
        return LoggerFactory.Create(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Trace);
            logging.AddZLoggerFile(sessionLogPath, UsePlainTextFormatter);

            if(!OperatingSystem.IsWindows() || WindowsConsole.TryAttachStandardOutput())
            {
                logging.AddZLoggerConsole(UsePlainTextFormatter);
            }
        });
    }

    private static void UsePlainTextFormatter(ZLoggerOptions options)
    {
        options.UsePlainTextFormatter(formatter =>
        {
            // "hh:mm:ss.fff [Info] message"
            formatter.SetPrefixFormatter($"{0:HH:mm:ss.fff} [{1}] ",
                (in template, in info) => template.Format(info.Timestamp, GetLevelName(info.LogLevel))
            );
        });
    }

    private static string GetLevelName(LogLevel level) => level switch
    {
        LogLevel.Trace => "Trace",
        LogLevel.Debug => "Debug",
        LogLevel.Information => "Info",
        LogLevel.Warning => "Warn",
        LogLevel.Error => "Error",
        LogLevel.Critical => "Crit",
        _ => "None"
    };
}
