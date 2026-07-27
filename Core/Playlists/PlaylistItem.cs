using Core.Playback;

namespace Core.Playlists;

public class PlaylistItem
{
    public IAudioTrack AudioTrack { get; private set; }
    public Playlist Playlist { get; set; }
    public int Index { get; set; }

    public PlaylistItem(IAudioTrack audioTrack, Playlist playlist, int index)
    {
        this.AudioTrack = audioTrack;
        Playlist = playlist;
        this.Index = index;
    }

    public PlaylistItem? GetNext()
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

    public PlaylistItem? GetPrevious()
    {
        int previousIndex = Index - 1;

        if(previousIndex > 0)
        {
            return Playlist.GetItem(previousIndex);
        }
        else
        {
            return null;
        }
    }
}
