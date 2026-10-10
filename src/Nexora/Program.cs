using Avalonia;
using System;
using System.Threading;

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

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        var builder = AppBuilder.Configure<App>().UsePlatformDetect();

        if(IsWaylandSession)
        {
            builder = builder.UseWayland();
        }

        return builder
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
    }
}
