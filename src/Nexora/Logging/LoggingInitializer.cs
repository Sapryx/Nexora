using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Text;
using Core.Logging;
using Microsoft.Extensions.Logging;
using ZLogger;
using AvaloniaLogger = Avalonia.Logging.Logger;

namespace Nexora.Logging;

public static class LoggingInitializer
{
    private const int KeptSessionLogCount = 10;
    private const string ResetColor = "\u001b[0m";
    private static readonly string LogsDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.DoNotVerify), "Nexora/logs");

    public static ILoggerFactory Initialize()
    {
        Directory.CreateDirectory(LogsDirectory);

        string sessionLogPath = SessionLogFiles.GetPath(LogsDirectory, DateTime.Now);
        var loggerFactory = CreateLoggerFactory(sessionLogPath);

        AvaloniaLogger.Sink = new AvaloniaLogSink(loggerFactory.CreateLogger<AvaloniaLogSink>());
        Trace.Listeners.Add(new DiagnosticsLogListener(loggerFactory.CreateLogger<DiagnosticsLogListener>()));
        SessionLogFiles.DeleteOld(sessionLogPath, KeptSessionLogCount, loggerFactory.CreateLogger(typeof(SessionLogFiles)));

        return loggerFactory;
    }

    public static ILoggerFactory CreateLoggerFactory(string sessionLogPath)
    {
        return LoggerFactory.Create(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Trace);
            logging.AddFilter(typeof(DiscordRpcLogger).FullName, LogLevel.Information);
            logging.AddZLoggerFile(sessionLogPath, UsePlainTextFormatter);

            if(!OperatingSystem.IsWindows() || WindowsConsole.TryAttachStandardOutput())
            {
                logging.AddZLoggerConsole(options =>
                {
                    options.ConfigureEnableAnsiEscapeCode = true;
                    UseColoredFormatter(options);
                });
            }
        });
    }

    public static void UseColoredFormatter(ZLoggerOptions options)
    {
        options.UsePlainTextFormatter(formatter =>
        {
            formatter.SetPrefixFormatter($"{0}{1:HH:mm:ss.fff} [{2}] ",
                (in template, in info) => template.Format(GetLevelColor(info.LogLevel), info.Timestamp, GetLevelName(info.LogLevel))
            );
            formatter.SetSuffixFormatter($"{0}",
                (in template, in info) => template.Format(info.Exception is null ? ResetColor : "")
            );
            formatter.SetExceptionFormatter((writer, exception) =>
                writer.Write(Encoding.UTF8.GetBytes($"{Environment.NewLine}{exception}{ResetColor}"))
            );
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

    private static string GetLevelColor(LogLevel level) => level switch
    {
        LogLevel.Trace or LogLevel.Debug => "\u001b[90m",
        LogLevel.Warning => "\u001b[33m",
        LogLevel.Error => "\u001b[31m",
        LogLevel.Critical => "\u001b[97;41m",
        _ => ""
    };
}
