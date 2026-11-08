using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Playback;
using Core.Playlists;
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
    public partial double Progress { get; set; }

    public bool IsInQueue { get; }
    private readonly IPlaylistItem playlistItem;
    private readonly PlaybackQueue playbackQueue;
    private readonly IAudioPlayer audioPlayer;
    private readonly TrackEditorVm trackEditorVm;

    public TrackControlVm(
        IPlaylistItem playlistItem,
        PlaybackQueue playbackQueue,
        IAudioPlayer audioPlayer,
        TrackEditorVm trackEditorVm,
        ICoverCache coverCache,
        IUiDispatcher uiDispatcher) : base(coverCache, uiDispatcher)
    {
        this.playlistItem = playlistItem;
        this.audioPlayer = audioPlayer;
        this.playbackQueue = playbackQueue;
        this.trackEditorVm = trackEditorVm;

        IsInQueue = playlistItem.Playlist == playbackQueue;

        ApplyState(audioPlayer.State);
        ShowPosition(audioPlayer.PlaybackPosition);

        var duration = playlistItem.AudioTrack.Metadata.Duration;
        UpdateTrackInfo();
        Duration = $"{(int)duration.TotalMinutes:00}:{duration.Seconds:00}";
    }

    public void ApplyState(PlaybackState state)
    {
        bool wasActive = IsActive;
        IsActive = state.Track == playlistItem;
        IsActiveAndPlaying = IsActive && state.Status == PlaybackStatus.Playing;

        if(IsActive != wasActive)
        {
            Progress = 0;
        }
    }

    public void ShowPosition(float position)
    {
        if(IsActive && IsInQueue)
        {
            Progress = position;
        }
    }

    [RelayCommand]
    public void PressPlayButton()
    {
        if(audioPlayer.State.Track == playlistItem)
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
        trackEditorVm.Open(playlistItem.AudioTrack, Cover);
    }

    public void UpdateTrackInfo()
    {
        var metadata = playlistItem.AudioTrack.Metadata;
        SetTrackInfo(metadata.Title, metadata.JoinedArtists, playlistItem.AudioTrack.AudioPath);
    }
}
