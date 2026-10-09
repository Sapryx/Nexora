using Core.Playlists;
using Nexora.Media;
using Nexora.Threading;

namespace Nexora.ViewModels;

public partial class TrackViewVm : CoveredTrackVm
{
    public TrackViewVm(ICoverCache coverCache, IUiDispatcher uiDispatcher) : base(coverCache, uiDispatcher)
    {
    }

    public void Update(IPlaylistItem playlistItem)
    {
        var metadata = playlistItem.AudioTrack.Metadata;
        SetTrackInfo(metadata.Title, metadata.Artists, playlistItem.AudioTrack.AudioPath);
    }
}
