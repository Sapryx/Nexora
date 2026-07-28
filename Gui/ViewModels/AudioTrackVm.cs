using System.IO;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Commands;
using Core.Playback;
using Core.Playlists;

namespace Gui.ViewModels;

public partial class AudioTrackVm : ViewModelBase
{
    [ObservableProperty]
    public partial bool IsActiveAndPlaying { get; set; }
    
    [ObservableProperty]
    public partial bool IsActive { get; set; }

    [ObservableProperty]
    public partial string Title { get; set; }
    
    [ObservableProperty]
    public partial string Artists { get; set; }
    
    [ObservableProperty]
    public partial string Duration { get; set; }

    [ObservableProperty]
    public partial Bitmap? TrackCover { get; set; }
    
    private readonly IPlaylistItem playlistItem;
    private readonly IPlayTrackCommand playTrackCommand;
    private readonly IAudioPlayer audioPlayer;

    public AudioTrackVm(
        IPlaylistItem playlistItem,
        IPlayTrackCommand playTrackCommand,
        IAudioPlayer audioPlayer)
    {
        this.playlistItem = playlistItem;
        this.playTrackCommand = playTrackCommand;
        this.audioPlayer = audioPlayer;

        this.IsActive = audioPlayer.NowPlaying == playlistItem;
        this.IsActiveAndPlaying = IsActive && audioPlayer.IsPlaying;
        this.Title = playlistItem.AudioTrack.Metadata.Title;
        this.Artists = playlistItem.AudioTrack.Metadata.Artists;
        this.Duration = $"{playlistItem.AudioTrack.Properties.Duration.TotalMinutes:00}:" +
                        $"{playlistItem.AudioTrack.Properties.Duration.Seconds:00}";

        var tackCoverRaw = playlistItem.AudioTrack.Metadata.TrackCoverRaw;

        if(tackCoverRaw != null)
        {
            using(var albumCoverStream = new MemoryStream(tackCoverRaw))
            {
                TrackCover = Bitmap.DecodeToWidth(albumCoverStream, 128, BitmapInterpolationMode.HighQuality);
            }
        }

        audioPlayer.PlaybackStarted += () =>
        {
            IsActive = audioPlayer.NowPlaying == this.playlistItem;
            IsActiveAndPlaying = IsActive;
        };

        audioPlayer.PlaybackPaused += () =>
        {
            IsActiveAndPlaying = false;
        };
    }

    [RelayCommand]
    public void PressPlayButton()
    {
        playTrackCommand.Execute(playlistItem);
    }
}
