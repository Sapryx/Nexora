using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Core.Integrations;
using Core.Logging;
using Core.Playlists;
using Core.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexora.Logging;
using Nexora.ViewModels;
using Nexora.Views;

namespace Nexora;

public partial class App : Application
{
    private static ServiceProvider provider = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var builder = new ServiceCollection();
        LoggingInitializer.Initialize(builder);
        RegisterDiContainer(builder);
        LogEnvironment();
        InitializeMainWindowVm();
        provider.GetService<IRichPresenceService>()?.Initialize();
        LibVLCSharp.Shared.Core.Initialize();

        base.OnFrameworkInitializationCompleted();
    }

    private void RegisterDiContainer(ServiceCollection builder)
    {
        CompositionRoot.Configure(builder);
        provider = builder.BuildServiceProvider();
    }

    private static void LogEnvironment()
    {
        var logger = provider.GetRequiredService<ILogger<App>>();

        logger.Info($"OS: {RuntimeInformation.OSDescription} ({RuntimeInformation.OSArchitecture}), .NET {Environment.Version}");
        logger.Info($"Windowing backend: {GetWindowingBackendName()}");

        if(OperatingSystem.IsLinux())
        {
            logger.Info($"Session type: {GetVariable("XDG_SESSION_TYPE")}, desktop: {GetVariable("XDG_CURRENT_DESKTOP")}, WAYLAND_DISPLAY: {GetVariable("WAYLAND_DISPLAY")}, DISPLAY: {GetVariable("DISPLAY")}");
        }
    }

    private static string GetWindowingBackendName()
    {
        if(OperatingSystem.IsWindows())
        {
            return "Win32";
        }

        if(OperatingSystem.IsMacOS())
        {
            return "macOS";
        }

        return Program.IsWaylandSession ? "Wayland" : "X11";
    }

    private static string GetVariable(string name)
    {
        return Environment.GetEnvironmentVariable(name) ?? "<unset>";
    }

    private void InitializeMainWindowVm()
    {
        if(ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }

        var audioTrackLoaders = provider.GetServices<ITrackLoader>();
        var playlistRegistry = provider.GetRequiredService<PlaylistRegistry>();

        var mainWindowVm = provider.GetRequiredService<MainWindowVm>();
        mainWindowVm.Initialize();

        desktop.MainWindow = new MainWindow()
        {
            DataContext = mainWindowVm
        };

        var logger = provider.GetRequiredService<ILogger<App>>();

        foreach(var loader in audioTrackLoaders)
        {
            Task.Run(() =>
            {
                try
                {
                    var loadedTracks = loader.Load();
                    playlistRegistry.GlobalPlaylist.AddTracks(loadedTracks);
                }
                catch(Exception ex)
                {
                    logger.Crit(ex, $"");
                }
            });
        }
    }
}
