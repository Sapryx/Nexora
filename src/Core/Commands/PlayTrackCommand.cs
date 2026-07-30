using Core.Playback;
using Core.Playlists;

namespace Core.Commands;

public class PlayTrackCommand : IPlayTrackCommand
{
    private readonly IAudioPlayer audioPlayer;

    public PlayTrackCommand(IAudioPlayer audioPlayer)
    {
        this.audioPlayer = audioPlayer;
    }

    public void Execute(IPlaylistItem playlistItem)
    {
        audioPlayer.PlayTrack(playlistItem);
    }
}
