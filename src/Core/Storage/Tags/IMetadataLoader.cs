using Core.Playback;

namespace Core.Storage.Tags;

public interface IMetadataLoader
{
    public Metadata Load(string filePath);
}
