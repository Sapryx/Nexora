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

    public bool IsInQueue { get; }
    private readonly IPlaylistItem playlistItem;
    private readonly PlaybackQueue playbackQueue;
    private readonly IAudioPlayer audioPlayer;

    public TrackControlVm(
        IPlaylistItem playlistItem,
        PlaybackQueue playbackQueue,
        IAudioPlayer audioPlayer,
        ICoverCache coverCache,
        IUiDispatcher uiDispatcher) : base(coverCache, uiDispatcher)
    {
        this.playlistItem = playlistItem;
        this.audioPlayer = audioPlayer;
        this.playbackQueue = playbackQueue;

        IsInQueue = playlistItem.Playlist == playbackQueue;

        IsActive = audioPlayer.NowPlaying == playlistItem;
        IsActiveAndPlaying = IsActive && audioPlayer.IsPlaying;

        var metadata = playlistItem.AudioTrack.Metadata;
        var duration = metadata.Duration;
        SetTrackInfo(metadata.Title, metadata.Artists, playlistItem.AudioTrack.AudioPath);
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
}
