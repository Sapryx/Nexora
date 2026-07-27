namespace Core.Commands;

public interface IChangeVolumeCommand
{
    void Execute(int newVolume);
}
