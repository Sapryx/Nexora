using CommunityToolkit.Mvvm.Input;
using Core.Playlists;
using Nexora.Media;
using Nexora.Threading;

namespace Nexora.ViewModels;

public partial class TrackViewVm : CoveredTrackVm
{
    private readonly CoverViewerVm coverViewerVm;

    public TrackViewVm(
        ICoverCache coverCache,
        CoverViewerVm coverViewerVm,
        IUiDispatcher uiDispatcher) : base(coverCache, uiDispatcher)
    {
        this.coverViewerVm = coverViewerVm;
    }

    public void Update(IPlaylistItem playlistItem)
    {
        var metadata = playlistItem.AudioTrack.Metadata;
        SetTrackInfo(metadata.Title, metadata.JoinedArtists, playlistItem.AudioTrack.AudioPath);
    }

    [RelayCommand]
    public void ShowCover()
    {
        var cover = Cover;

        if(cover != null)
        {
            coverViewerVm.Open(AudioPath, cover);
        }
    }
}
