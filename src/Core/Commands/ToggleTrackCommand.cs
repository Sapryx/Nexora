using Core.Playback;
using Core.Playlists;

namespace Core.Commands;

public class ToggleTrackCommand : IToggleTrackCommand
{
    private readonly IAudioPlayer audioPlayer;
    private readonly IPlayTrackCommand playTrackCommand;

    public ToggleTrackCommand(
        IAudioPlayer audioPlayer, 
        IPlayTrackCommand playTrackCommand)
    {
        this.audioPlayer = audioPlayer;
        this.playTrackCommand = playTrackCommand;
    }

    public void Execute(IPlaylistItem playlistItem)
    {
        if(audioPlayer.NowPlaying == playlistItem)
        {
            audioPlayer.TogglePause();
        }
        else
        {
            playTrackCommand.Execute(playlistItem);
        }
    }
}
