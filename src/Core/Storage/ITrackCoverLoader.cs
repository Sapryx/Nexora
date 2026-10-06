namespace Core.Storage;

public interface ITrackCoverLoader
{
    public byte[]? LoadCover(string audioPath);
}
