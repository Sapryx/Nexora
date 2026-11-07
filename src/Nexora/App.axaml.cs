using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Core.Integrations;
using Core.Library;
using Core.Logging;
using Core.Playback.Loudness;
using Core.Playlists;
using Core.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nexora.Animations;
using Nexora.Render;
using Nexora.Theme.Resources;
using Nexora.ViewModels;
using Nexora.Views;

namespace Nexora;

public partial class App : Application
{
    private static ServiceProvider provider = null!;
    private readonly ILoggerFactory loggerFactory;

    public App() : this(NullLoggerFactory.Instance)
    {
    }

    public App(ILoggerFactory loggerFactory)
    {
        this.loggerFactory = loggerFactory;
    }

    public override void Initialize()
    {
        LoadAnimationResources();
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var builder = new ServiceCollection();
        builder.AddSingleton(loggerFactory);
        builder.AddLogging();
        RegisterDiContainer(builder);
        LogEnvironment();
        InitializeMainWindowVm();
        provider.GetService<IRichPresenceService>()?.Initialize();
        LibVLCSharp.Shared.Core.Initialize();

        base.OnFrameworkInitializationCompleted();
    }

    private void LoadAnimationResources()
    {
        var loader = new AnimationSettingsLoader(
            ReadDefaultAnimationSettings(),
            CompositionRoot.AnimationsFilePath,
            loggerFactory.CreateLogger<AnimationSettingsLoader>()
        );
        var factory = new AnimationResourceFactory(loggerFactory.CreateLogger<AnimationResourceFactory>());
        AnimationResources.Entries = factory.Create(loader.Load(), loader.LoadDefaults());
    }

    private static string ReadDefaultAnimationSettings()
    {
        using var stream = AssetLoader.Open(new Uri("avares://Nexora/Assets/animations.json"));
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private void RegisterDiContainer(ServiceCollection builder)
    {
        CompositionRoot.Configure(builder);
        provider = builder.BuildServiceProvider();
    }

    private static void LogEnvironment()
    {
        var logger = provider.GetRequiredService<ILogger<App>>();

        logger.Info(
            $"OS: {RuntimeInformation.OSDescription} ({RuntimeInformation.OSArchitecture}), " +
            $".NET {Environment.Version}"
        );
        logger.Info($"Windowing backend: {GetWindowingBackendName()}");

        if(OperatingSystem.IsLinux())
        {
            logger.Info(
                $"Session type: {GetVariable("XDG_SESSION_TYPE")}, " +
                $"desktop: {GetVariable("XDG_CURRENT_DESKTOP")}, " +
                $"WAYLAND_DISPLAY: {GetVariable("WAYLAND_DISPLAY")}, " +
                $"DISPLAY: {GetVariable("DISPLAY")}"
            );
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
        var artistRegistry = provider.GetRequiredService<ArtistRegistry>();
        var loudnessScanner = provider.GetRequiredService<LoudnessScanner>();

        var mainWindowVm = provider.GetRequiredService<MainWindowVm>();
        mainWindowVm.Initialize();

        var mainWindow = new MainWindow()
        {
            DataContext = mainWindowVm
        };
        provider.GetRequiredService<RenderScaleProvider>().Track(mainWindow);
        desktop.MainWindow = mainWindow;

        var logger = provider.GetRequiredService<ILogger<App>>();

        foreach(var loader in audioTrackLoaders)
        {
            Task.Run(async () =>
            {
                try
                {
                    var loadedTracks = loader.Load();
                    artistRegistry.AddTracks(loadedTracks);
                    playlistRegistry.GlobalPlaylist.AddTracks(loadedTracks);
                    await loudnessScanner.ScanAsync(loadedTracks);
                }
                catch(Exception ex)
                {
                    logger.Error(ex, $"Failed to load tracks");
                }
            });
        }
    }
}
