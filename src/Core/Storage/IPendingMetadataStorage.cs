namespace Core.Storage;

public interface IPendingMetadataStorage
{
    public List<MetadataChange> LoadAll();
    public void Save(MetadataChange change);
    public void Delete(string audioPath);
}
