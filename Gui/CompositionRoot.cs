using Core;
using Core.Commands;
using Core.Integrations;
using Core.Playback;
using Core.Playlists;
using Core.Storage;
using Gui.ViewModels;
using Gui.ViewModels.Factories;
using Infrastructure;
using Infrastructure.Integrations;
using Infrastructure.Playback;
using Infrastructure.Storage;
using LibVLCSharp.Shared;
using Microsoft.Extensions.DependencyInjection;

namespace Gui;

public static class CompositionRoot
{
    public static void Configure(ServiceCollection builder)
    {
        var vlc = new LibVLC("--no-video");
        builder.AddSingleton(vlc);

        builder.AddSingleton<IPlayTrackCommand, PlayTrackCommand>();
        builder.AddSingleton<IChangeVolumeCommand, ChangeVolumeCommand>();
        builder.AddSingleton<IPlayNextTrackCommand, PlayNextTrackCommand>();
        builder.AddSingleton<IPauseTrackCommand, PauseTrackCommand>();
        builder.AddSingleton<IPlayPreviousTrackCommand, PlayPreviousTrackCommand>();
        
        builder.AddSingleton<IAudioTrackVmFactory, AudioTrackVmFactory>();
        builder.AddSingleton<IAudioTrackLoader, FileAudioTrackLoader>();
        builder.AddSingleton<IAudioEngine, VlcAudioEngine>();
        builder.AddSingleton<IMetadataLoader, TagLibMetadataLoader>();
        builder.AddSingleton<IRpcService, DiscordRpcService>();
        builder.AddSingleton<AudioPlayer>();
        builder.AddSingleton<PlaylistRegistry>();

        builder.AddSingleton<MainWindowVm>();
    }
}
