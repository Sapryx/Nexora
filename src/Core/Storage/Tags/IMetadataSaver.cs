namespace Core.Storage.Tags;

public interface IMetadataSaver
{
    public void Save(MetadataChange change);
}
