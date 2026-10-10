using Core.Storage;
using Microsoft.Extensions.Logging;
using Tests.Shared.Logging;

namespace Core.Tests.Storage;

public class MusicDirectoryProviderTests : IDisposable
{
    private readonly TestLogger<MusicDirectoryProvider> logger = new TestLogger<MusicDirectoryProvider>();
    private readonly string directory = Path.Combine(Path.GetTempPath(), $"nexora-music-{Guid.NewGuid()}");

    public void Dispose()
    {
        if(Directory.Exists(directory))
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void GetFiles_DirectoryExists_ReturnsItsFiles()
    {
        Directory.CreateDirectory(directory);
        string file = Path.Combine(directory, "one.mp3");
        File.WriteAllBytes(file, []);
        var provider = new MusicDirectoryProvider(logger, directory);

        var files = provider.GetFiles();

        Assert.Equal([file], files);
    }

    [Fact]
    public void GetFiles_DirectoryDoesNotExist_ReturnsEmpty()
    {
        var provider = new MusicDirectoryProvider(logger, directory);

        var files = provider.GetFiles();

        Assert.Empty(files);
    }

    [Fact]
    public void GetFiles_DirectoryDoesNotExist_LogsWarningWithPath()
    {
        var provider = new MusicDirectoryProvider(logger, directory);

        provider.GetFiles();

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Contains(directory, entry.Message);
    }

    [Fact]
    public void GetFiles_PathIsEmpty_ReturnsEmpty()
    {
        var provider = new MusicDirectoryProvider(logger, "");

        var files = provider.GetFiles();

        Assert.Empty(files);
    }
}
