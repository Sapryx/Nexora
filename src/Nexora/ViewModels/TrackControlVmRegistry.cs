using Core.Library;
using Core.Playback;
using Core.Playlists;
using Nexora.Media;
using Nexora.Threading;

namespace Nexora.ViewModels;

public class TrackControlVmRegistry
{
    private readonly PlaybackQueue playbackQueue;
    private readonly IAudioPlayer audioPlayer;
    private readonly TrackEditorVm trackEditorVm;
    private readonly ICoverCache coverCache;
    private readonly IUiDispatcher uiDispatcher;
    private readonly Dictionary<IPlaylistItem, TrackControlVm> trackVms = [];
    private TrackControlVm? activeTrackVm;

    public TrackControlVmRegistry(
        PlaybackQueue playbackQueue,
        IAudioPlayer audioPlayer,
        TrackTagWriter tagWriter,
        TrackEditorVm trackEditorVm,
        ICoverCache coverCache,
        IUiDispatcher uiDispatcher)
    {
        this.playbackQueue = playbackQueue;
        this.audioPlayer = audioPlayer;
        this.trackEditorVm = trackEditorVm;
        this.coverCache = coverCache;
        this.uiDispatcher = uiDispatcher;

        audioPlayer.StateChanged += state => uiDispatcher.Post(() =>
        {
            activeTrackVm?.ApplyState(state);
            activeTrackVm = state.Track != null ? trackVms.GetValueOrDefault(state.Track) : null;
            activeTrackVm?.ApplyState(state);
        });

        audioPlayer.PlaybackPositionChanged += position => uiDispatcher.Post(() =>
        {
            activeTrackVm?.ShowPosition(position);
        });

        tagWriter.TrackChanged += audioTrack => uiDispatcher.Post(() =>
        {
            foreach(var pair in trackVms.Where(it => it.Key.AudioTrack == audioTrack))
            {
                pair.Value.UpdateTrackInfo();
            }
        });

        playbackQueue.ItemRemoved += playlistItem => uiDispatcher.Post(() => trackVms.Remove(playlistItem));
    }

    public TrackControlVm Create(IPlaylistItem playlistItem)
    {
        var trackVm = new TrackControlVm(
            playlistItem,
            playbackQueue,
            audioPlayer,
            trackEditorVm,
            coverCache,
            uiDispatcher
        );
        trackVms[playlistItem] = trackVm;

        if(trackVm.IsActive)
        {
            activeTrackVm = trackVm;
        }

        return trackVm;
    }
}
