using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace Nexora.Logging;

public static class LoggingInitializer
{
    public static readonly string LogsDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Nexora/logs");

    public static void Initialize(ServiceCollection builder)
    {
        ConfigureLogging(builder);
        ConfigureCrashLogging();
    }

    private static void ConfigureLogging(ServiceCollection builder)
    {
        Directory.CreateDirectory(LogsDirectory);

        var sessionLogPath = Path.Combine(LogsDirectory, "session.log");

        if(File.Exists(sessionLogPath))
        {
            File.Delete(sessionLogPath);
        }

        builder.AddLogging(logging =>
        {
            logging.ClearProviders();
            logging.SetMinimumLevel(LogLevel.Trace);

            logging.AddZLoggerFile(sessionLogPath, options =>
            {
                options.UsePlainTextFormatter(formatter =>
                {
                    // "hh:mm:ss [Info] message"
                    formatter.SetPrefixFormatter($"{0} [{1}] ",
                        (in MessageTemplate template, in LogInfo info) =>
                            template.Format(DateTime.Now.ToString("HH:mm:ss"), GetLevelName(info.LogLevel))
                    );
                });
            });
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

    private static void ConfigureCrashLogging()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            WriteCrashLog(e.ExceptionObject as Exception, "AppDomain.UnhandledException");
        };
        
        Dispatcher.UIThread.UnhandledException += (_, e) =>
        {
            WriteCrashLog(e.Exception, "Avalonia.UIThread");
        };

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            WriteCrashLog(e.Exception, "TaskScheduler.UnobservedTaskException");
            e.SetObserved();
        };
    }

    private static void WriteCrashLog(Exception? ex, string source)
    {
        Directory.CreateDirectory(LogsDirectory);

        string currentDateString = $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}";
        string crashLogPath = Path.Combine(LogsDirectory, $"crash-{currentDateString}.log");

        string entry =
            $"{currentDateString} [CRT] ({source}){Environment.NewLine}" +
            $"{ex}{Environment.NewLine}{new string('-', 60)}{Environment.NewLine}";

        File.AppendAllText(crashLogPath, entry);
    }
}
