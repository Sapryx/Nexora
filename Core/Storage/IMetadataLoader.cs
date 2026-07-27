namespace Core.Storage;

public interface IMetadataLoader
{
    public Metadata LoadMetadata(string filePath);
}
