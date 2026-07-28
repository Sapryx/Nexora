using Core.Playback;
using Core.Playlists;

namespace Core.Commands;

public class ToggleTrackCommand : IToggleTrackCommand
{
    private readonly IAudioPlayer audioPlayer;
    private readonly IPlayTrackCommand playTrackCommand;
    private readonly IPauseTrackCommand pauseTrackCommand;

    public ToggleTrackCommand(
        IAudioPlayer audioPlayer, 
        IPlayTrackCommand playTrackCommand,
        IPauseTrackCommand pauseTrackCommand)
    {
        this.audioPlayer = audioPlayer;
        this.playTrackCommand = playTrackCommand;
        this.pauseTrackCommand = pauseTrackCommand;
    }

    public void Execute(IPlaylistItem playlistItem)
    {
        if(audioPlayer.NowPlaying == playlistItem)
        {
            pauseTrackCommand.Execute();
        }
        else
        {
            playTrackCommand.Execute(playlistItem);
        }
    }
}
