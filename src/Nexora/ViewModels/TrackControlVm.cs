using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Playback;
using Core.Playlists;
using Core.Storage;
using Nexora.Media;
using Nexora.Threading;

namespace Nexora.ViewModels;

public partial class TrackControlVm : CoveredTrackVm
{
    [ObservableProperty]
    public partial string Duration { get; set; } = "";

    [ObservableProperty]
    public partial bool IsActiveAndPlaying { get; set; }

    [ObservableProperty]
    public partial bool IsActive { get; set; }

    [ObservableProperty]
    public partial bool HasUnsavedChanges { get; set; }

    [ObservableProperty]
    public partial string EditedTitle { get; set; } = "";

    [ObservableProperty]
    public partial string EditedArtists { get; set; } = "";

    public bool IsInQueue { get; }
    private readonly IPlaylistItem playlistItem;
    private readonly PlaybackQueue playbackQueue;
    private readonly IAudioPlayer audioPlayer;
    private readonly TrackMetadataEditor metadataEditor;
    private readonly TrackEditorVm trackEditorVm;

    public TrackControlVm(
        IPlaylistItem playlistItem,
        PlaybackQueue playbackQueue,
        IAudioPlayer audioPlayer,
        TrackMetadataEditor metadataEditor,
        TrackEditorVm trackEditorVm,
        ICoverCache coverCache,
        IUiDispatcher uiDispatcher) : base(coverCache, uiDispatcher)
    {
        this.playlistItem = playlistItem;
        this.audioPlayer = audioPlayer;
        this.playbackQueue = playbackQueue;
        this.metadataEditor = metadataEditor;
        this.trackEditorVm = trackEditorVm;

        IsInQueue = playlistItem.Playlist == playbackQueue;

        IsActive = audioPlayer.NowPlaying == playlistItem;
        IsActiveAndPlaying = IsActive && audioPlayer.IsPlaying;

        var duration = playlistItem.AudioTrack.Metadata.Duration;
        UpdateTrackInfo();
        Duration = $"{(int)duration.TotalMinutes:00}:{duration.Seconds:00}";

        audioPlayer.PlaybackStarted += nowPlaying => uiDispatcher.Post(() =>
        {
            IsActive = nowPlaying == playlistItem;
            IsActiveAndPlaying = IsActive;
        });

        audioPlayer.PlaybackPaused += () => uiDispatcher.Post(() =>
        {
            IsActiveAndPlaying = false;
        });

        audioPlayer.PlaybackStopped += () => uiDispatcher.Post(() =>
        {
            IsActive = false;
            IsActiveAndPlaying = false;
        });

        metadataEditor.MetadataChanged += audioPath => uiDispatcher.Post(() =>
        {
            if(audioPath == playlistItem.AudioTrack.AudioPath)
            {
                UpdateTrackInfo();
            }
        });
    }

    [RelayCommand]
    public void PressPlayButton()
    {
        if(audioPlayer.NowPlaying == playlistItem)
        {
            audioPlayer.TogglePause();
        }
        else
        {
            audioPlayer.PlayTrack(playlistItem);
        }
    }

    [RelayCommand]
    public void AddToQueue()
    {
        playbackQueue.AddTrack(playlistItem.AudioTrack);
    }

    [RelayCommand]
    public void RemoveFromQueue()
    {
        playbackQueue.RemoveItem(playlistItem);
    }

    [RelayCommand]
    public void Edit()
    {
        trackEditorVm.Open(this);
    }

    public void BeginEdit()
    {
        EditedTitle = Title;
        EditedArtists = Artists;
    }

    public Task ApplyEditAsync()
    {
        return metadataEditor.EditAsync(playlistItem.AudioTrack, EditedTitle, EditedArtists);
    }

    private void UpdateTrackInfo()
    {
        var metadata = playlistItem.AudioTrack.Metadata;
        SetTrackInfo(metadata.Title, metadata.Artists, playlistItem.AudioTrack.AudioPath);
        HasUnsavedChanges = metadataEditor.HasUnsavedChanges(playlistItem.AudioTrack.AudioPath);
    }
}
