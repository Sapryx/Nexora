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
using Nexora.Input;
using Nexora.Media;
using Nexora.Render;
using Nexora.Threading;
using Nexora.ViewModels;

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
    public static readonly string? AnimationsFilePath = null;
#else
    public static readonly string? AnimationsFilePath = Path.Combine(AppDataDirectory, "animations.json");
#endif

    public static void Configure(ServiceCollection builder)
    {
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
        builder.AddSingleton<PlaylistRegistry>();
        builder.AddSingleton<ArtistRegistry>();
        builder.AddSingleton<PlaybackQueue>();
        builder.AddSingleton<KeyboardLayoutTranslator>();

        if(OperatingSystem.IsWindows())
        {
            builder.AddSingleton<IKeyboardLayoutProvider, WindowsKeyboardLayoutProvider>();
        }
        else if(Program.IsWaylandSession)
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

    private static LibVLC CreateLibVlc(LibVlcLogForwarder logForwarder)
    {
        var vlc = new LibVLC("--no-video");
        vlc.Log += (_, e) => logForwarder.Forward(e.Level, e.Module, e.Message);

        return vlc;
    }
}
