using System;
using Core.Integrations;
using Core.Logging;
using Core.Playback;
using Core.Playlists;
using Core.Search;
using Core.Storage;
using LibVLCSharp.Shared;
using Microsoft.Extensions.DependencyInjection;
using Nexora.Input;
using Nexora.Media;
using Nexora.Threading;
using Nexora.ViewModels;
using Nexora.ViewModels.Factories;

namespace Nexora;

public static class CompositionRoot
{
    public static void Configure(ServiceCollection builder)
    {
        builder.AddSingleton<LibVlcLogForwarder>();
        builder.AddSingleton(provider => CreateLibVlc(provider.GetRequiredService<LibVlcLogForwarder>()));
        builder.AddSingleton<IUiDispatcher, AvaloniaUiDispatcher>();
        builder.AddSingleton<ITrackControlVmFactory, TrackControlVmFactory>();
        builder.AddSingleton<ITrackLoader, FileTrackLoader>();
        builder.AddSingleton<IDegreeOfParallelismProvider<FileTrackLoader>, FileTrackLoaderDegreeOfParallelismProvider>();
        builder.AddSingleton<ISupportedAudioFormatsProvider, SupportedAudioFormatsProvider>();
        builder.AddSingleton<IMusicDirectoryProvider, MusicDirectoryProvider>();
        builder.AddSingleton<IMetadataLoader, TagLibMetadataLoader>();
        builder.AddSingleton<ITrackCoverLoader, TagLibTrackCoverLoader>();
        builder.AddSingleton<IRichPresenceService, DiscordRichPresenceService>();
        builder.AddSingleton<IAudioPlayer, AudioPlayer>();
        builder.AddSingleton<ICoverCache, CoverCache>();
        builder.AddSingleton<PlaylistRegistry>();
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
    }

    private static LibVLC CreateLibVlc(LibVlcLogForwarder logForwarder)
    {
        var vlc = new LibVLC("--no-video");
        vlc.Log += (_, e) => logForwarder.Forward(e.Level, e.Module, e.Message);

        return vlc;
    }
}
