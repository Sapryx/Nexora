using System;
using System.Threading.Tasks;
using Avalonia.Threading;
using Core.Logging;
using Microsoft.Extensions.Logging;

namespace Nexora.Logging;

public class CrashLogger
{
    private readonly ILoggerFactory loggerFactory;
    private readonly ILogger logger;
    private Exception? lastUiThreadException;

    public CrashLogger(ILoggerFactory loggerFactory)
    {
        this.loggerFactory = loggerFactory;
        logger = loggerFactory.CreateLogger<CrashLogger>();
    }

    public void Register()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) => LogUnhandledException((Exception)e.ExceptionObject);
        AppDomain.CurrentDomain.ProcessExit += (_, _) => loggerFactory.Dispose();

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            LogUnobservedTaskException(e.Exception);
            e.SetObserved();
        };
    }

    public void RegisterUiThread()
    {
        Dispatcher.UIThread.UnhandledException += (_, e) => LogUiThreadException(e.Exception);
    }

    public void LogUnhandledException(Exception exception)
    {
        if(exception != lastUiThreadException)
        {
            logger.Crit(exception, $"Unhandled exception, the application is terminating");
        }

        loggerFactory.Dispose();
    }

    public void LogUiThreadException(Exception exception)
    {
        lastUiThreadException = exception;
        logger.Crit(exception, $"Unhandled exception on the UI thread");
    }

    public void LogUnobservedTaskException(AggregateException exception)
    {
        logger.Error(exception, $"Unobserved task exception");
    }
}
