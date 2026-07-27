using Core.Playlists;

namespace Core.Commands;

public interface IPlayTrackCommand
{
    public void Execute(PlaylistItem playlistItem);
}
