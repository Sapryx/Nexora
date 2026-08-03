using Core.Commands;
using Core.Integrations;
using Core.Playback;
using Core.Playlists;
using Core.Storage;
using Infrastructure.Integrations;
using Infrastructure.Playback;
using Infrastructure.Storage;
using LibVLCSharp.Shared;
using Microsoft.Extensions.DependencyInjection;
using Nexora.ViewModels;
using Nexora.ViewModels.Factories;

namespace Nexora;

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
        builder.AddSingleton<ITrackLoader, FileTrackLoader>();
        builder.AddSingleton<IAudioEngine, VlcAudioEngine>();
        builder.AddSingleton<IDegreeOfParallelismProvider<FileTrackLoader>, FileTrackLoaderDegreeOfParallelismProvider>();
        builder.AddSingleton<ISupportedAudioFormatsProvider, SupportedAudioFormatsProvider>();
        builder.AddSingleton<IMetadataLoader, TagLibMetadataLoader>();
        builder.AddSingleton<ITrackPropertyLoader, TagLibTrackPropertyLoader>();
        builder.AddSingleton<IRichPresenceService, DiscordRichPresenceService>();
        builder.AddSingleton<IAudioPlayer, AudioPlayer>();
        builder.AddSingleton<PlaylistRegistry>();

        builder.AddSingleton<MainWindowVm>();
        builder.AddSingleton<SearchBarVm>();
        builder.AddSingleton<PlaybackVm>();
    }
}
