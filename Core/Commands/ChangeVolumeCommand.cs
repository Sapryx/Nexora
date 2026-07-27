namespace Core.Commands;

public class ChangeVolumeCommand : IChangeVolumeCommand
{
    private readonly IAudioEngine audioEngine;

    public ChangeVolumeCommand(IAudioEngine audioEngine)
    {
        this.audioEngine = audioEngine;
    }

    public void Execute(int newVolume)
    {
        int clippedValue = Math.Max(newVolume, 0);
        audioEngine.Volume = clippedValue;
    }
}
