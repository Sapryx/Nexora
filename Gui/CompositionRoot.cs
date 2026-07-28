using System;
using Core.Commands;
using Core.Integrations;
using Core.Playback;
using Core.Playlists;
using Core.Storage;
using Gui.ViewModels;
using Gui.ViewModels.Factories;
using Infrastructure.Integrations;
using Infrastructure.Playback;
using Infrastructure.Storage;
using LibVLCSharp.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
        builder.AddSingleton<IToggleTrackCommand, ToggleTrackCommand>();
        
        builder.AddSingleton<IAudioTrackVmFactory, AudioTrackVmFactory>();
        builder.AddSingleton<ITrackLoader, FileTrackLoader>(provider =>
            // Degree of parallelism here is a result of benchmarking.
            new FileTrackLoader(
                provider.GetService<ILogger<FileTrackLoader>>()!,
                provider.GetService<IMetadataLoader>()!, 
                provider.GetService<ITrackPropertyLoader>()!, 
                Environment.ProcessorCount * 2)
        );
        builder.AddSingleton<IAudioEngine, VlcAudioEngine>();
        builder.AddSingleton<IMetadataLoader, TagLibMetadataLoader>();
        builder.AddSingleton<ITrackPropertyLoader, TagLibTrackPropertyLoader>();
        builder.AddSingleton<IRichPresenceService, DiscordRichPresenceService>();
        builder.AddSingleton<IAudioPlayer, AudioPlayer>();
        builder.AddSingleton<PlaylistRegistry>();

        builder.AddSingleton<MainWindowVm>();
    }
}
