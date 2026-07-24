using System.IO;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core;
using Core.Commands;

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
    public partial Bitmap? AlbumCover { get; set; }
    
    private readonly PlaylistItem playlistItem;
    private readonly IPlayAudioTrackCommand playAudioTrackCommand;
    private readonly AudioPlayer audioPlayer;

    public AudioTrackVm(
        PlaylistItem playlistItem,
        IPlayAudioTrackCommand playAudioTrackCommand,
        AudioPlayer audioPlayer)
    {
        this.playlistItem = playlistItem;
        this.playAudioTrackCommand = playAudioTrackCommand;
        this.audioPlayer = audioPlayer;

        this.IsActive = audioPlayer.NowPlaying == playlistItem;
        this.IsActiveAndPlaying = IsActive && audioPlayer.IsPlaying;
        this.Title = playlistItem.AudioTrack.Metadata.Title;
        this.Artists = string.Join(", ", playlistItem.AudioTrack.Metadata.Artists);
        this.Duration = $"{playlistItem.AudioTrack.Metadata.Duration.TotalMinutes:00}:{playlistItem.AudioTrack.Metadata.Duration.Seconds:00}";

        var albumCoverRaw = playlistItem.AudioTrack.Metadata.AlbumCoverRaw;

        if(albumCoverRaw != null)
        {
            using(var albumCoverStream = new MemoryStream(albumCoverRaw))
            {
                AlbumCover = new Bitmap(albumCoverStream);
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
        playAudioTrackCommand.Execute(playlistItem);
    }
}
