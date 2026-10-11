using Core.Playback;
using Core.Playlists;
using Nexora.Media;
using Nexora.Threading;

namespace Nexora.ViewModels.Factories;

public class TrackControlVmFactory : ITrackControlVmFactory
{
    private readonly PlaybackQueue playbackQueue;
    private readonly IAudioPlayer audioPlayer;
    private readonly ICoverCache coverCache;
    private readonly IUiDispatcher uiDispatcher;

    public TrackControlVmFactory(
        PlaybackQueue playbackQueue,
        IAudioPlayer audioPlayer,
        ICoverCache coverCache,
        IUiDispatcher uiDispatcher)
    {
        this.playbackQueue = playbackQueue;
        this.audioPlayer = audioPlayer;
        this.coverCache = coverCache;
        this.uiDispatcher = uiDispatcher;
    }

    public TrackControlVm Create(IPlaylistItem playlistItem)
    {
        return new TrackControlVm(playlistItem, playbackQueue, audioPlayer, coverCache, uiDispatcher);
    }
}
