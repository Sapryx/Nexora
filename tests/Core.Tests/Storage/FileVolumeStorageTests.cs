using Core.Storage;
using Microsoft.Extensions.Logging;
using Tests.Shared.Logging;

namespace Core.Tests.Storage;

public class FileVolumeStorageTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), $"nexora-volume-{Guid.NewGuid()}");
    private readonly string filePath;
    private readonly TestLogger<FileVolumeStorage> logger = new TestLogger<FileVolumeStorage>();
    private readonly FileVolumeStorage storage;

    public FileVolumeStorageTests()
    {
        filePath = Path.Combine(directory, "volume.txt");
        storage = new FileVolumeStorage(filePath, logger);
    }

    public void Dispose()
    {
        if(Directory.Exists(directory))
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void Load_NoFile_ReturnsNull()
    {
        Assert.Null(storage.Load());
    }

    [Fact]
    public void Save_MissingDirectory_CreatesFileThatLoadReads()
    {
        storage.Save(42);

        Assert.Equal(42, storage.Load());
    }

    [Fact]
    public void Save_Twice_LoadReturnsLastValue()
    {
        storage.Save(80);
        storage.Save(15);

        Assert.Equal(15, storage.Load());
    }

    [Theory]
    [InlineData("150", 100)]
    [InlineData("-5", 0)]
    [InlineData(" 30\n", 30)]
    public void Load_SavedText_ReturnsClampedVolume(string text, int expected)
    {
        WriteFile(text);

        Assert.Equal(expected, storage.Load());
    }

    [Fact]
    public void Load_NotANumber_ReturnsNullAndWarns()
    {
        WriteFile("loud");

        Assert.Null(storage.Load());
        Assert.Contains(logger.Entries, it => it.Level == LogLevel.Warning);
    }

    private void WriteFile(string text)
    {
        Directory.CreateDirectory(directory);
        File.WriteAllText(filePath, text);
    }
}
