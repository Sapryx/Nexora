using Core.Playback;

namespace Core.Commands;

public class ChangeVolumeCommand : IChangeVolumeCommand
{
    private readonly IAudioPlayer audioPlayer;

    public ChangeVolumeCommand(IAudioPlayer audioPlayer)
    {
        this.audioPlayer = audioPlayer;
    }

    public void Execute(int newVolume)
    {
        int clippedValue = Math.Max(newVolume, 0);
        audioPlayer.Volume = clippedValue;
    }
}
