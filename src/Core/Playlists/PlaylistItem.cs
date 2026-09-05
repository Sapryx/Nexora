using Core.Playback;

namespace Core.Playlists;

public class PlaylistItem : IPlaylistItem
{
    public IAudioTrack AudioTrack { get; }
    public Playlist Playlist { get; set; }
    public int Index { get; set; }

    public PlaylistItem(IAudioTrack audioTrack, Playlist playlist, int index)
    {
        AudioTrack = audioTrack;
        Playlist = playlist;
        Index = index;
    }

    public IPlaylistItem? GetNext()
    {
        int nextIndex = Index + 1;

        if(nextIndex < Playlist.TrackCount)
        {
            return Playlist.GetItem(nextIndex);
        }
        else
        {
            return null;
        }
    }

    public IPlaylistItem? GetPrevious()
    {
        int previousIndex = Index - 1;

        if(previousIndex >= 0)
        {
            return Playlist.GetItem(previousIndex);
        }
        else
        {
            return null;
        }
    }
}
