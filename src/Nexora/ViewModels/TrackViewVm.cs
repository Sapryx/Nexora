using Core.Playlists;
using Nexora.Media;

namespace Nexora.ViewModels;

public partial class TrackViewVm : CoveredTrackVm
{
    public TrackViewVm(ICoverCache coverCache) : base(coverCache)
    {
    }

    public void Update(IPlaylistItem playlistItem)
    {
        var metadata = playlistItem.AudioTrack.Metadata;
        SetTrackInfo(metadata.Title, metadata.Artists, playlistItem.AudioTrack.AudioPath);
    }
}
