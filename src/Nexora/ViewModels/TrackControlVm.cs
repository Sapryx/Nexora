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

    private readonly IPlaylistItem playlistItem;
    private readonly IAudioPlayer audioPlayer;

    public TrackControlVm(
        IPlaylistItem playlistItem,
        IAudioPlayer audioPlayer,
        ICoverCache coverCache,
        IUiDispatcher uiDispatcher) : base(coverCache, uiDispatcher)
    {
        this.playlistItem = playlistItem;
        this.audioPlayer = audioPlayer;

        IsActive = audioPlayer.NowPlaying == playlistItem;
        IsActiveAndPlaying = IsActive && audioPlayer.IsPlaying;

        var metadata = playlistItem.AudioTrack.Metadata;
        var duration = metadata.Duration;
        SetTrackInfo(metadata.Title, metadata.Artists, playlistItem.AudioTrack.AudioPath);
        Duration = $"{duration.TotalMinutes:00}:{duration.Seconds:00}";

        audioPlayer.PlaybackStarted += () => uiDispatcher.Post(() =>
        {
            IsActive = audioPlayer.NowPlaying == playlistItem;
            IsActiveAndPlaying = IsActive;
        });

        audioPlayer.PlaybackPaused += () => uiDispatcher.Post(() =>
        {
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
}
