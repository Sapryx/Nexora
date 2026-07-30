using Core.Playback;

namespace Core.Commands;

public class PlayPreviousTrackCommand : IPlayPreviousTrackCommand
{
    private readonly IAudioPlayer audioPlayer;
    private readonly IPlayTrackCommand playTrackCommand;

    public PlayPreviousTrackCommand(IAudioPlayer audioPlayer, IPlayTrackCommand playTrackCommand)
    {
        this.audioPlayer = audioPlayer;
        this.playTrackCommand = playTrackCommand;
    }

    public void Execute()
    {
        var previous = audioPlayer.NowPlaying?.GetPrevious();
        
        if(previous != null)
        {
            playTrackCommand.Execute(previous);
        }
    }
}
