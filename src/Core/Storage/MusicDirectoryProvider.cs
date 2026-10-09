namespace Core.Storage;

public class MusicDirectoryProvider : IMusicDirectoryProvider
{
    public IEnumerable<string> GetFiles()
    {
        string musicDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
        return Directory.EnumerateFiles(musicDirectory);
    }
}
