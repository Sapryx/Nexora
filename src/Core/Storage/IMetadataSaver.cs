namespace Core.Storage;

public interface IMetadataSaver
{
    public void Save(MetadataChange change);
}
