using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Text;
using Core.Logging;
using Microsoft.Extensions.Logging;
using ZLogger;
using ZLogger.Providers;
using AvaloniaLogger = Avalonia.Logging.Logger;

namespace Nexora.Logging;

public static class LoggingInitializer
{
    private const int KeptSessionLogCount = 10;
    private const LogLevel DefaultMinimumLevel = LogLevel.Information;
    private const string LogLevelLegendCategory = "Nexora.Logging.LogLevelLegend";
    private const string ColoredLogLevelLegendCategory = "Nexora.Logging.ColoredLogLevelLegend";
    private const string CurrentLevelMarker = "<--";
    private const string CurrentLevelMarkerColor = "\e[38;2;0;255;0m";
    private static readonly LogLevel[] LegendLevels = [LogLevel.Trace, LogLevel.Debug, LogLevel.Information, LogLevel.Warning, LogLevel.Error, LogLevel.Critical];
    private const string ResetColor = "\e[0m";
    private static readonly string LogsDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.DoNotVerify), "Nexora/logs");

    public static ILoggerFactory Initialize(string[] args)
    {
        Directory.CreateDirectory(LogsDirectory);

        string? requestedLevel = LogLevelArgument.FindValue(args);
        var minimumLevel = DefaultMinimumLevel;
        bool isRequestedLevelUnknown = false;

        if(requestedLevel != null)
        {
            if(LogLevelArgument.TryParse(requestedLevel, out var level))
            {
                minimumLevel = level;
            }
            else
            {
                isRequestedLevelUnknown = true;
            }
        }

        string sessionLogPath = SessionLogFiles.GetPath(LogsDirectory, DateTime.Now);
        var loggerFactory = CreateLoggerFactory(sessionLogPath, minimumLevel);
        var logger = loggerFactory.CreateLogger(typeof(LoggingInitializer));

        LogLevelLegend(loggerFactory, minimumLevel);

        if(isRequestedLevelUnknown)
        {
            logger.Warn($"Unknown log level '{requestedLevel}' in {LogLevelArgument.Name}, expected one of: {LogLevelArgument.KnownNames}");
        }

        AvaloniaLogger.Sink = new AvaloniaLogSink(loggerFactory.CreateLogger<AvaloniaLogSink>());
        Trace.Listeners.Add(new DiagnosticsLogListener(loggerFactory.CreateLogger<DiagnosticsLogListener>()));
        SessionLogFiles.DeleteOld(sessionLogPath, KeptSessionLogCount, loggerFactory.CreateLogger(typeof(SessionLogFiles)));

        return loggerFactory;
    }

    public static void LogLevelLegend(ILoggerFactory loggerFactory, LogLevel minimumLevel)
    {
        var fileLogger = loggerFactory.CreateLogger(LogLevelLegendCategory);
        var consoleLogger = loggerFactory.CreateLogger(ColoredLogLevelLegendCategory);

        fileLogger.Info($"Minimum log level:");
        consoleLogger.Info($"Minimum log level:");

        foreach(var level in LegendLevels)
        {
            bool isCurrent = level == minimumLevel;

            LogAtLevel(fileLogger, level, isCurrent ? CurrentLevelMarker : "");
            LogAtLevel(consoleLogger, level, isCurrent ? ResetColor + CurrentLevelMarkerColor + CurrentLevelMarker : "");
        }
    }

    public static ILoggerFactory CreateLoggerFactory(string sessionLogPath, LogLevel minimumLevel)
    {
        return LoggerFactory.Create(logging =>
        {
            logging.SetMinimumLevel(minimumLevel);
            logging.AddFilter(typeof(DiscordRpcLogger).FullName, LogLevel.Information);
            logging.AddFilter(LogLevelLegendCategory, LogLevel.Trace);
            logging.AddFilter(ColoredLogLevelLegendCategory, LogLevel.Trace);
            logging.AddFilter<ZLoggerConsoleLoggerProvider>(LogLevelLegendCategory, LogLevel.None);
            logging.AddFilter<ZLoggerFileLoggerProvider>(ColoredLogLevelLegendCategory, LogLevel.None);
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
            // "hh:mm:ss.fff [Info ] message"
            formatter.SetPrefixFormatter($"{0:HH:mm:ss.fff} [{1}] ",
                (in template, in info) => template.Format(info.Timestamp, GetLevelName(info.LogLevel))
            );
        });
    }

    private static string GetLevelName(LogLevel level) => level switch
    {
        LogLevel.Trace => "Trace",
        LogLevel.Debug => "Debug",
        LogLevel.Information => "Info ",
        LogLevel.Warning => "Warn ",
        LogLevel.Error => "Error",
        LogLevel.Critical => "Crit ",
        _ => "None "
    };

    private static void LogAtLevel(ILogger logger, LogLevel level, string message)
    {
        switch(level)
        {
            case LogLevel.Trace:
                logger.Trace($"{message}");
                break;
            case LogLevel.Debug:
                logger.Debug($"{message}");
                break;
            case LogLevel.Information:
                logger.Info($"{message}");
                break;
            case LogLevel.Warning:
                logger.Warn($"{message}");
                break;
            case LogLevel.Error:
                logger.Error($"{message}");
                break;
            case LogLevel.Critical:
                logger.Crit($"{message}");
                break;
        }
    }

    private static string GetLevelColor(LogLevel level) => level switch
    {
        LogLevel.Trace or LogLevel.Debug => "\u001b[90m",
        LogLevel.Warning => "\u001b[33m",
        LogLevel.Error => "\u001b[31m",
        LogLevel.Critical => "\u001b[97;41m",
        _ => ""
    };
}
