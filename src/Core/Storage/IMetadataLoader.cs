namespace Core.Storage;

public interface IMetadataLoader
{
    public Metadata Load(string filePath);
}
