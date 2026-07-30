using Core.Playback;

namespace Core.Commands;

public class PauseTrackCommand : IPauseTrackCommand
{
    private readonly IAudioPlayer audioPlayer;

    public PauseTrackCommand(IAudioPlayer audioPlayer)
    {
        this.audioPlayer = audioPlayer;
    }

    public void Execute()
    {
        audioPlayer.TogglePause();
    }
}
