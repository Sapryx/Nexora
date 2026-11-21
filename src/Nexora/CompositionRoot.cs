using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using Core.Input;
using Core.Integrations;
using Core.Library;
using Core.Logging;
using Core.Playback;
using Core.Playback.Loudness;
using Core.Playlists;
using Core.Storage;
using Core.Storage.Tags;
using LibVLCSharp.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nexora.Animations;
using Nexora.Input;
using Nexora.Logging;
using Nexora.Media;
using Nexora.Render;
using Nexora.Theme.Resources;
using Nexora.Threading;
using Nexora.ViewModels;
using Nexora.Views;

namespace Nexora;

public static class CompositionRoot
{
    private static readonly string AppDataDirectory = Path.Combine(
        Environment.GetFolderPath(
            Environment.SpecialFolder.ApplicationData,
            Environment.SpecialFolderOption.DoNotVerify
        ),
        "Nexora"
    );

    private static readonly string VolumeFilePath = Path.Combine(AppDataDirectory, "volume.txt");
#if DEBUG
    private static readonly string? AnimationsFilePath = null;
#else
    private static readonly string? AnimationsFilePath = Path.Combine(AppDataDirectory, "animations.json");
#endif

    private static readonly bool IsWaylandSession =
        OperatingSystem.IsLinux() && Environment.GetEnvironmentVariable("WAYLAND_DISPLAY") != null;

    public static void Run(string[] args)
    {
        using var loggerFactory = LoggingInitializer.Initialize(args);
        var crashLogger = new CrashLogger(loggerFactory);
        crashLogger.Register();

        var builder = new ServiceCollection();
        builder.AddSingleton(loggerFactory);
        builder.AddLogging();
        Configure(builder);
        var provider = builder.BuildServiceProvider();

        BuildAvaloniaApp(() => CreateApp(loggerFactory, () => Start(provider)))
            .AfterSetup(_ => crashLogger.RegisterUiThread(provider.GetRequiredService<IUiDispatcher>()))
            .StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildDesignerApp()
    {
        return BuildAvaloniaApp(() => CreateApp(NullLoggerFactory.Instance, () => new MainWindow())).LogToTrace();
    }

    public static void LoadAnimationResources(ILoggerFactory loggerFactory)
    {
        var loader = new AnimationSettingsLoader(
            ReadDefaultAnimationSettings(),
            AnimationsFilePath,
            loggerFactory.CreateLogger<AnimationSettingsLoader>()
        );
        var factory = new AnimationResourceFactory(loggerFactory.CreateLogger<AnimationResourceFactory>());
        AnimationResources.Entries = factory.Create(loader.Load(), loader.LoadDefaults());
    }

    public static void Configure(ServiceCollection builder)
    {
        builder.AddSingleton(provider => new EnvironmentLogger(
            provider.GetRequiredService<ILogger<EnvironmentLogger>>(),
            IsWaylandSession
        ));
        builder.AddSingleton<LibVlcLogForwarder>();
        builder.AddSingleton(provider => CreateLibVlc(provider.GetRequiredService<LibVlcLogForwarder>()));
        builder.AddSingleton<IUiDispatcher, AvaloniaUiDispatcher>();
        builder.AddSingleton<TrackControlVmRegistry>();
        builder.AddSingleton<ITrackLoader, FileTrackLoader>();
        builder.AddSingleton<
            IDegreeOfParallelismProvider<FileTrackLoader>,
            FileTrackLoaderDegreeOfParallelismProvider
        >();
        builder.AddSingleton<ISupportedAudioFormatsProvider, SupportedAudioFormatsProvider>();
        builder.AddSingleton<IMusicDirectoryProvider, MusicDirectoryProvider>();
        builder.AddSingleton<IMetadataLoader, TagLibMetadataLoader>();
        builder.AddSingleton<IMetadataSaver, TagLibMetadataSaver>();
        builder.AddSingleton<ILoudnessAnalyzer, VlcLoudnessAnalyzer>();
        builder.AddSingleton<LoudnessScanner>();
        builder.AddSingleton<
            IDegreeOfParallelismProvider<LoudnessScanner>,
            LoudnessScannerDegreeOfParallelismProvider
        >();
        builder.AddSingleton<TrackTagWriter>();
        builder.AddSingleton<ITrackCoverLoader, TagLibTrackCoverLoader>();
        builder.AddSingleton<IDiscordClient, DiscordRpcClientAdapter>();
        builder.AddSingleton<IRichPresenceService, DiscordRichPresenceService>();
        builder.AddSingleton<IMediaEngine, VlcMediaEngine>();
        builder.AddSingleton<IAudioPlayer, AudioPlayer>();
        builder.AddSingleton<IVolumeStorage>(provider => new FileVolumeStorage(
            VolumeFilePath,
            provider.GetRequiredService<ILogger<FileVolumeStorage>>()
        ));
        builder.AddSingleton<RenderScaleProvider>();
        builder.AddSingleton<ICoverCache, CoverCache>();
        builder.AddSingleton<MusicLibrary>();
        builder.AddSingleton<PlaybackQueue>();
        builder.AddSingleton<KeyboardLayoutTranslator>();

        if(OperatingSystem.IsWindows())
        {
            builder.AddSingleton<IKeyboardLayoutProvider, WindowsKeyboardLayoutProvider>();
        }
        else if(IsWaylandSession)
        {
            builder.AddSingleton<IKeyboardLayoutProvider, WaylandKeyboardLayoutProvider>();
        }
        else
        {
            builder.AddSingleton<IKeyboardLayoutProvider, UnsupportedKeyboardLayoutProvider>();
        }

        builder.AddSingleton<MainWindowVm>();
        builder.AddSingleton<SearchBarVm>();
        builder.AddSingleton<PlaybackVm>();
        builder.AddSingleton<QueueVm>();
        builder.AddSingleton<TrackEditorVm>();
        builder.AddSingleton<CoverViewerVm>();
    }

    private static App CreateApp(ILoggerFactory loggerFactory, Func<Window> start)
    {
        return new App()
        {
            LoadResources = () => LoadAnimationResources(loggerFactory),
            Start = start
        };
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
            .With(new SkiaOptions() { UseOpacitySaveLayer = true })
            .WithInterFont();
    }

    private static string ReadDefaultAnimationSettings()
    {
        using var stream = AssetLoader.Open(new Uri("avares://Nexora/Assets/animations.json"));
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static Window Start(ServiceProvider provider)
    {
        provider.GetRequiredService<EnvironmentLogger>().Log();

        var mainWindowVm = provider.GetRequiredService<MainWindowVm>();
        mainWindowVm.Initialize();

        var mainWindow = new MainWindow()
        {
            DataContext = mainWindowVm
        };
        provider.GetRequiredService<RenderScaleProvider>().Track(mainWindow);
        _ = provider.GetRequiredService<MusicLibrary>().LoadAsync();
        provider.GetRequiredService<IRichPresenceService>().Initialize();
        LibVLCSharp.Shared.Core.Initialize();

        return mainWindow;
    }

    private static LibVLC CreateLibVlc(LibVlcLogForwarder logForwarder)
    {
        var vlc = new LibVLC("--no-video");
        vlc.Log += (_, e) => logForwarder.Forward(e.Level, e.Module, e.Message);

        return vlc;
    }
}
