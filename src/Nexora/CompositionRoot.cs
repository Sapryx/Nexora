using Core.Integrations;
using Core.Playback;
using Core.Playlists;
using Core.Storage;
using LibVLCSharp.Shared;
using Microsoft.Extensions.DependencyInjection;
using Nexora.Media;
using Nexora.Threading;
using Nexora.ViewModels;
using Nexora.ViewModels.Factories;

namespace Nexora;

public static class CompositionRoot
{
    public static void Configure(ServiceCollection builder)
    {
        var vlc = new LibVLC("--no-video");
        builder.AddSingleton(vlc);
        
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

        builder.AddSingleton<MainWindowVm>();
        builder.AddSingleton<SearchBarVm>();
        builder.AddSingleton<PlaybackVm>();
    }
}
