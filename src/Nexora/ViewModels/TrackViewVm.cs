using System.IO;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Playlists;

namespace Nexora.ViewModels;

public partial class TrackViewVm : ViewModelBase
{
    [ObservableProperty]
    public partial Bitmap? Cover { get; set; }

    [ObservableProperty]
    public partial string Title { get; set; }

    [ObservableProperty]
    public partial string Artists { get; set; }
    
    public void Update(IPlaylistItem playlistItem)
    {
        Title = playlistItem.AudioTrack.Metadata.Title;
        Artists = playlistItem.AudioTrack.Metadata.Artists;
        
        var coverRaw = playlistItem.AudioTrack.Metadata.TrackCoverRaw;

        if(coverRaw != null)
        {
            using(var albumCoverStream = new MemoryStream(coverRaw))
            {
                Cover = Bitmap.DecodeToWidth(albumCoverStream, 128, BitmapInterpolationMode.HighQuality);
            }
        }
    }
}
