using Core;
using Core.Commands;
using Gui.ViewModels;
using Gui.ViewModels.Factories;
using Infrastructure;
using LibVLCSharp.Shared;
using Microsoft.Extensions.DependencyInjection;

namespace Gui;

public static class CompositionRoot
{
    public static void Configure(ServiceCollection builder)
    {
        var vlc = new LibVLC("--no-video");
        builder.AddSingleton(vlc);

        builder.AddSingleton<IPlayAudioTrackCommand, PlayAudioTrackCommand>();
        builder.AddSingleton<IChangeAudioVolumeCommand, ChangeAudioVolumeCommand>();
        builder.AddSingleton<IPlayNextAudioTrackCommand, PlayNextAudioTrackCommand>();
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
