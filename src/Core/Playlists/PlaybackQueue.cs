using Core.Playback;

namespace Core.Playlists;

public class PlaybackQueue : Playlist
{
    private readonly IAudioPlayer audioPlayer;

    public PlaybackQueue(IAudioPlayer audioPlayer)
    {
        this.audioPlayer = audioPlayer;
    }

    public override void RemoveItem(IPlaylistItem item)
    {
        if(item.Playlist == this && audioPlayer.NowPlaying == item)
        {
            PlayNextOrStop(item);
        }

        base.RemoveItem(item);
    }

    private void PlayNextOrStop(IPlaylistItem item)
    {
        var next = item.GetNext();

        if(next != null)
        {
            audioPlayer.PlayTrack(next);
        }
        else
        {
            audioPlayer.Stop();
        }
    }
}
