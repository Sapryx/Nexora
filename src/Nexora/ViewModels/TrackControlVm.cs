using System.IO;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Playback;
using Core.Playlists;

namespace Nexora.ViewModels;

public partial class TrackControlVm : ViewModelBase
{
    [ObservableProperty]
    public partial Bitmap? Cover { get; set; }

    [ObservableProperty]
    public partial string Title { get; set; }

    [ObservableProperty]
    public partial string Artists { get; set; }

    [ObservableProperty]
    public partial string Duration { get; set; }
    
    [ObservableProperty]
    public partial bool IsActiveAndPlaying { get; set; }
    
    [ObservableProperty]
    public partial bool IsActive { get; set; }

    private readonly IAudioPlayer audioPlayer;
    private IPlaylistItem? playlistItem;

    public TrackControlVm(IAudioPlayer audioPlayer)
    {
        this.audioPlayer = audioPlayer;

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

    public async Task SetTrack(IPlaylistItem item)
    {
        playlistItem = item;
        IsActive = audioPlayer.NowPlaying == playlistItem;
        IsActiveAndPlaying = IsActive && audioPlayer.IsPlaying;
        
        Title = playlistItem.AudioTrack.Metadata.Title;
        Artists = playlistItem.AudioTrack.Metadata.Artists;
        Duration = $"{playlistItem.AudioTrack.Properties.Duration.TotalMinutes:00}:" +
                   $"{playlistItem.AudioTrack.Properties.Duration.Seconds:00}";
        
        var coverRaw = playlistItem.AudioTrack.Metadata.TrackCoverRaw;

        if(coverRaw == null)
        {
            return;
        }

        await Task.Run(() =>
        {
            using var albumCoverStream = new MemoryStream(coverRaw);
            Cover = Bitmap.DecodeToWidth(albumCoverStream, 128, BitmapInterpolationMode.HighQuality);
        });
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
