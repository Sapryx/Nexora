namespace Core.Storage.Tags;

public interface ITrackCoverLoader
{
    public byte[]? LoadCover(string audioPath);
}
