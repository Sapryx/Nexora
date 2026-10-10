namespace Core.Storage;

public interface IVolumeStorage
{
    public int? Load();
    public void Save(int volume);
}
