using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Playback;
using Core.Playlists;
using Nexora.Media;

namespace Nexora.ViewModels;

public partial class TrackControlVm : CoveredTrackVm
{
    [ObservableProperty]
    public partial string Duration { get; set; } = "";

    [ObservableProperty]
    public partial bool IsActiveAndPlaying { get; set; }
    
    [ObservableProperty]
    public partial bool IsActive { get; set; }

    private readonly IAudioPlayer audioPlayer;
    private IPlaylistItem? playlistItem;

    public TrackControlVm(IAudioPlayer audioPlayer, ICoverCache coverCache) : base(coverCache)
    {
        this.audioPlayer = audioPlayer;

        audioPlayer.PlaybackStarted += () =>
        {
            IsActive = audioPlayer.NowPlaying == playlistItem;
            IsActiveAndPlaying = IsActive;
        };

        audioPlayer.PlaybackPaused += () =>
        {
            IsActiveAndPlaying = false;
        };
    }

    public void SetTrack(IPlaylistItem item)
    {
        playlistItem = item;
        IsActive = audioPlayer.NowPlaying == item;
        IsActiveAndPlaying = IsActive && audioPlayer.IsPlaying;

        var metadata = item.AudioTrack.Metadata;
        var duration = metadata.Duration;
        SetTrackInfo(metadata.Title, metadata.Artists, item.AudioTrack.AudioPath);
        Duration = $"{duration.TotalMinutes:00}:{duration.Seconds:00}";
    }

    [RelayCommand]
    public void PressPlayButton()
    {
        if(audioPlayer.NowPlaying == playlistItem!)
        {
            audioPlayer.TogglePause();
        }
        else
        {
            audioPlayer.PlayTrack(playlistItem!);
        }
    }
}
