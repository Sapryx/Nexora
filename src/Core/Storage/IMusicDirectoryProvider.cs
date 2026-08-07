namespace Core.Storage;

public interface IMusicDirectoryProvider
{
    public IEnumerable<string> GetFiles();
}
