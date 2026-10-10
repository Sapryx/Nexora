using Avalonia;
using System;
using System.Threading;
using Nexora.Logging;

namespace Nexora;

sealed class Program
{
    public static bool IsWaylandSession => OperatingSystem.IsLinux() && Environment.GetEnvironmentVariable("WAYLAND_DISPLAY") != null;
    private const string InstanceMutexName = "Nexora.SingleInstance";

    [STAThread]
    public static void Main(string[] args)
    {
        var mutexOptions = new NamedWaitHandleOptions() { CurrentUserOnly = true, CurrentSessionOnly = false };
        using var instanceMutex = new Mutex(true, InstanceMutexName, mutexOptions, out bool isFirstInstance);

        if(!isFirstInstance)
        {
            return;
        }

        using var loggerFactory = LoggingInitializer.Initialize(args);
        var crashLogger = new CrashLogger(loggerFactory);
        crashLogger.Register();

        BuildAvaloniaApp(() => new App(loggerFactory))
            .AfterSetup(_ => crashLogger.RegisterUiThread())
            .StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        return BuildAvaloniaApp(() => new App()).LogToTrace();
    }

    private static AppBuilder BuildAvaloniaApp(Func<App> createApp)
    {
        var builder = AppBuilder.Configure(createApp).UsePlatformDetect();

        if(IsWaylandSession)
        {
            builder = builder.UseWayland();
        }

        return builder
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont();
    }
}
