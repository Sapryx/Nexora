using Core.Playlists;

namespace Core.Commands;

public interface IToggleTrackCommand
{
    public void Execute(IPlaylistItem playlistItem);
}
