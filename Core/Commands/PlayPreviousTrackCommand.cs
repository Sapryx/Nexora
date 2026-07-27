namespace Core.Commands;

public class PlayPreviousTrackCommand : IPlayPreviousTrackCommand
{
    private readonly AudioPlayer audioPlayer;
    private readonly IPlayAudioTrackCommand playAudioTrackCommand;

    public PlayPreviousTrackCommand(AudioPlayer audioPlayer, IPlayAudioTrackCommand playAudioTrackCommand)
    {
        this.audioPlayer = audioPlayer;
        this.playAudioTrackCommand = playAudioTrackCommand;
    }

    public void Execute()
    {
        var previous = audioPlayer.NowPlaying?.GetPrevious();
        
        if(previous != null)
        {
            playAudioTrackCommand.Execute(previous);
        }
    }
}
