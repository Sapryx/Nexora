using Core.Playback;
using Core.Playlists;
using Nexora.Media;
using Nexora.Threading;

namespace Nexora.ViewModels.Factories;

public class TrackControlVmFactory : ITrackControlVmFactory
{
    private readonly IAudioPlayer audioPlayer;
    private readonly ICoverCache coverCache;
    private readonly IUiDispatcher uiDispatcher;

    public TrackControlVmFactory(IAudioPlayer audioPlayer, ICoverCache coverCache, IUiDispatcher uiDispatcher)
    {
        this.audioPlayer = audioPlayer;
        this.coverCache = coverCache;
        this.uiDispatcher = uiDispatcher;
    }

    public TrackControlVm Create(IPlaylistItem playlistItem)
    {
        return new TrackControlVm(playlistItem, audioPlayer, coverCache, uiDispatcher);
    }
}
