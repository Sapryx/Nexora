namespace Core.Commands;

public class PauseTrackCommand : IPauseTrackCommand
{
    private readonly AudioPlayer audioPlayer;

    public PauseTrackCommand(AudioPlayer audioPlayer)
    {
        this.audioPlayer = audioPlayer;
    }

    public void Execute()
    {
        audioPlayer.TogglePause();
    }
}
