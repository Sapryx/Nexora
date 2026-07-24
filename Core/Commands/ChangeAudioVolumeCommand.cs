namespace Core.Commands;

public class ChangeAudioVolumeCommand : IChangeAudioVolumeCommand
{
    private readonly IAudioEngine audioEngine;

    public ChangeAudioVolumeCommand(IAudioEngine audioEngine)
    {
        this.audioEngine = audioEngine;
    }

    public void Execute(int newVolume)
    {
        int clippedValue = Math.Max(newVolume, 0);
        audioEngine.Volume = clippedValue;
    }
}
