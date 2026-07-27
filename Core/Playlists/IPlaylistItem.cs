using Core.Playback;

namespace Core.Playlists;

public interface IPlaylistItem
{
    IAudioTrack AudioTrack { get; }
    Playlist Playlist { get; set; }
    int Index { get; set; }
    IPlaylistItem? GetNext();
    IPlaylistItem? GetPrevious();
}
