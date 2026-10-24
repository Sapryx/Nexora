using Core.Playback;
using Core.Playlists;
using Core.Storage;
using Nexora.Media;
using Nexora.Threading;

namespace Nexora.ViewModels.Factories;

public class TrackControlVmFactory : ITrackControlVmFactory
{
    private readonly PlaybackQueue playbackQueue;
    private readonly IAudioPlayer audioPlayer;
    private readonly TrackMetadataEditor metadataEditor;
    private readonly TrackEditorVm trackEditorVm;
    private readonly ICoverCache coverCache;
    private readonly IUiDispatcher uiDispatcher;

    public TrackControlVmFactory(
        PlaybackQueue playbackQueue,
        IAudioPlayer audioPlayer,
        TrackMetadataEditor metadataEditor,
        TrackEditorVm trackEditorVm,
        ICoverCache coverCache,
        IUiDispatcher uiDispatcher)
    {
        this.playbackQueue = playbackQueue;
        this.audioPlayer = audioPlayer;
        this.metadataEditor = metadataEditor;
        this.trackEditorVm = trackEditorVm;
        this.coverCache = coverCache;
        this.uiDispatcher = uiDispatcher;
    }

    public TrackControlVm Create(IPlaylistItem playlistItem)
    {
        return new TrackControlVm(
            playlistItem,
            playbackQueue,
            audioPlayer,
            metadataEditor,
            trackEditorVm,
            coverCache,
            uiDispatcher
        );
    }
}
