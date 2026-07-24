namespace Core.Commands;

public interface IChangeAudioVolumeCommand
{
    void Execute(int newVolume);
}
