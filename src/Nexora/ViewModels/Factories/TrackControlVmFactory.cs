using Core.Playback;
using Core.Playlists;
using Nexora.Media;

namespace Nexora.ViewModels.Factories;

public class TrackControlVmFactory : ITrackControlVmFactory
{
    private readonly IAudioPlayer audioPlayer;
    private readonly ICoverCache coverCache;

    public TrackControlVmFactory(IAudioPlayer audioPlayer, ICoverCache coverCache)
    {
        this.audioPlayer = audioPlayer;
        this.coverCache = coverCache;
    }

    public TrackControlVm Create(IPlaylistItem playlistItem)
    {
        return new TrackControlVm(playlistItem, audioPlayer, coverCache);
    }
}
