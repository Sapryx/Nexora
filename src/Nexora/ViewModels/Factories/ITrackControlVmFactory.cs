using Core.Playlists;

namespace Nexora.ViewModels.Factories;

public interface ITrackControlVmFactory
{
    public TrackControlVm Create(IPlaylistItem playlistItem);
}
