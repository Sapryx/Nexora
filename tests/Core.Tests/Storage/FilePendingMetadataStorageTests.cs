using Core.Storage;
using Microsoft.Extensions.Logging;
using Tests.Shared.Logging;

namespace Core.Tests.Storage;

public class FilePendingMetadataStorageTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), $"nexora-pending-{Guid.NewGuid()}");
    private readonly TestLogger<FilePendingMetadataStorage> logger = new TestLogger<FilePendingMetadataStorage>();
    private readonly FilePendingMetadataStorage storage;

    public FilePendingMetadataStorageTests()
    {
        storage = new FilePendingMetadataStorage(directory, logger);
    }

    public void Dispose()
    {
        if(Directory.Exists(directory))
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void LoadAll_NoDirectory_ReturnsEmptyList()
    {
        Assert.Empty(storage.LoadAll());
    }

    [Fact]
    public void Save_MissingDirectory_CreatesChangeThatLoadAllReads()
    {
        storage.Save(new MetadataChange() { AudioPath = "/music/Psycho.mp3", Title = "Uprising" });

        var change = Assert.Single(storage.LoadAll());
        Assert.Equal("/music/Psycho.mp3", change.AudioPath);
        Assert.Equal("Uprising", change.Title);
        Assert.Null(change.Artists);
    }

    [Fact]
    public void Save_SameTrackTwice_KeepsLatestChangeOnly()
    {
        storage.Save(new MetadataChange() { AudioPath = "/music/Psycho.mp3", Title = "Uprising" });
        storage.Save(new MetadataChange() { AudioPath = "/music/Psycho.mp3", Artists = ["Muse"] });

        var change = Assert.Single(storage.LoadAll());
        Assert.Null(change.Title);
        Assert.Equal(["Muse"], change.Artists);
    }

    [Fact]
    public void Delete_SavedChange_RemovesOnlyThatTrack()
    {
        storage.Save(new MetadataChange() { AudioPath = "/music/Psycho.mp3", Title = "Uprising" });
        storage.Save(new MetadataChange() { AudioPath = "/music/Madness.mp3", Title = "Starlight" });

        storage.Delete("/music/Psycho.mp3");

        Assert.Equal(["/music/Madness.mp3"], storage.LoadAll().Select(it => it.AudioPath));
    }

    [Fact]
    public void Delete_NoDirectory_DoesNothing()
    {
        storage.Delete("/music/Psycho.mp3");

        Assert.False(Directory.Exists(directory));
    }

    [Fact]
    public void LoadAll_CorruptFile_WarnsAndSkipsIt()
    {
        storage.Save(new MetadataChange() { AudioPath = "/music/Psycho.mp3", Title = "Uprising" });
        File.WriteAllText(Path.Combine(directory, "corrupt.json"), "{ not json");

        var change = Assert.Single(storage.LoadAll());

        Assert.Equal("/music/Psycho.mp3", change.AudioPath);
        Assert.Equal(LogLevel.Warning, Assert.Single(logger.Entries).Level);
    }
}
