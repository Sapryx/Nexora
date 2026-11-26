using Avalonia;
using Avalonia.Headless.XUnit;
using Avalonia.Media.Imaging;
using Microsoft.Extensions.Logging;
using Nexora.Media;
using Tests.Shared.Logging;

namespace Nexora.UiTests;

public sealed class ThumbnailStoreTests : IDisposable
{
    private const int PixelSide = 64;
    private readonly string directory = TestMainWindow.CreateTemporaryDirectory();
    private readonly string audioPath;
    private readonly TestLogger<ThumbnailStore> logger = new TestLogger<ThumbnailStore>();
    private readonly ThumbnailStore store;

    public ThumbnailStoreTests()
    {
        audioPath = Path.Combine(directory, "Artist - Title.mp3");
        File.WriteAllBytes(audioPath, [1]);
        File.SetLastWriteTimeUtc(audioPath, DateTime.UtcNow.AddDays(-1));
        store = new ThumbnailStore(Path.Combine(directory, "cache"), logger);
    }

    [Fact]
    public void PathOf_NamesThumbnailAfterAudioFileAndPixelSize()
    {
        Assert.Equal(Path.Combine(directory, "cache", "Artist - Title 64x64.png"), store.PathOf(audioPath, PixelSide));
    }

    [Fact]
    public void IsFresh_NoThumbnail_ReturnsFalse()
    {
        Assert.False(store.IsFresh(audioPath, PixelSide));
    }

    [Fact]
    public void IsFresh_SavedAfterAudioFileChanged_ReturnsTrue()
    {
        store.Save(audioPath, PixelSide, null);
        Assert.True(store.IsFresh(audioPath, PixelSide));
    }

    [Fact]
    public void IsFresh_AudioFileChangedAfterSave_ReturnsFalse()
    {
        store.Save(audioPath, PixelSide, null);
        File.SetLastWriteTimeUtc(audioPath, DateTime.UtcNow.AddMinutes(1));

        Assert.False(store.IsFresh(audioPath, PixelSide));
    }

    [Fact]
    public void TryLoad_NoThumbnail_ReturnsFalse()
    {
        Assert.False(store.TryLoad(audioPath, PixelSide, out var thumbnail));
        Assert.Null(thumbnail);
    }

    [AvaloniaFact]
    public void TryLoad_SavedCover_ReturnsItAndLeavesOnlyThePng()
    {
        using var cover = CreateBitmap(new PixelSize(64, 32));
        store.Save(audioPath, PixelSide, cover);

        Assert.True(store.TryLoad(audioPath, PixelSide, out var thumbnail));
        Assert.Equal(new PixelSize(64, 32), Assert.IsType<Bitmap>(thumbnail).PixelSize);
        Assert.Equal([store.PathOf(audioPath, PixelSide)], Directory.GetFiles(Path.Combine(directory, "cache")));
    }

    [Fact]
    public void TryLoad_SavedMissingCover_ReturnsTrueWithoutThumbnail()
    {
        store.Save(audioPath, PixelSide, null);

        Assert.True(store.TryLoad(audioPath, PixelSide, out var thumbnail));
        Assert.Null(thumbnail);
    }

    [Fact]
    public void TryLoad_AudioFileChangedAfterSave_ReturnsFalse()
    {
        store.Save(audioPath, PixelSide, null);
        File.SetLastWriteTimeUtc(audioPath, DateTime.UtcNow.AddMinutes(1));

        Assert.False(store.TryLoad(audioPath, PixelSide, out _));
    }

    [AvaloniaFact]
    public void TryLoad_BrokenThumbnail_ReturnsFalseAndLogsWarning()
    {
        Directory.CreateDirectory(Path.Combine(directory, "cache"));
        File.WriteAllBytes(store.PathOf(audioPath, PixelSide), [1, 2, 3]);

        Assert.False(store.TryLoad(audioPath, PixelSide, out var thumbnail));
        Assert.Null(thumbnail);
        Assert.Equal(LogLevel.Warning, Assert.Single(logger.Entries).Level);
    }

    [Fact]
    public void Save_DirectoryCannotBeCreated_LogsWarning()
    {
        File.WriteAllBytes(Path.Combine(directory, "cache"), [1]);

        store.Save(audioPath, PixelSide, null);

        Assert.Equal(LogLevel.Warning, Assert.Single(logger.Entries).Level);
    }

    [Fact]
    public void Save_ThumbnailCannotBeReplaced_LogsWarningAndRemovesTemporaryFile()
    {
        Directory.CreateDirectory(store.PathOf(audioPath, PixelSide));

        store.Save(audioPath, PixelSide, null);

        Assert.Equal(LogLevel.Warning, Assert.Single(logger.Entries).Level);
        Assert.Empty(Directory.GetFiles(Path.Combine(directory, "cache")));
    }

    [Fact]
    public void RemoveOrphanedThumbnails_KeepsAllSizesForPresentTracksAndLeavesOtherFiles()
    {
        string orphan = Path.Combine(directory, "Artist - Title 96x96.mp3");
        string cache = Path.Combine(directory, "cache");
        store.Save(audioPath, 64, null);
        store.Save(audioPath, 240, null);
        store.Save(orphan, 64, null);
        store.Save(orphan, 240, null);
        string[] unrelated = [
            "artwork.png",
            "artwork 64x160.png",
            "artwork 0x0.png",
            "notes 9876543210x9876543210.png"
        ];

        foreach(string name in unrelated)
        {
            File.WriteAllText(Path.Combine(cache, name), name);
        }

        store.RemoveOrphanedThumbnails([audioPath], CancellationToken.None);

        Assert.True(File.Exists(store.PathOf(audioPath, 64)));
        Assert.True(File.Exists(store.PathOf(audioPath, 240)));
        Assert.False(File.Exists(store.PathOf(orphan, 64)));
        Assert.False(File.Exists(store.PathOf(orphan, 240)));
        Assert.All(unrelated, name => Assert.Equal(name, File.ReadAllText(Path.Combine(cache, name))));
    }

    [Fact]
    public void RemoveOrphanedThumbnails_Cancelled_DoesNotDeleteFiles()
    {
        store.Save(audioPath, PixelSide, null);

        var token = new CancellationToken(true);
        Assert.Throws<OperationCanceledException>(() => store.RemoveOrphanedThumbnails([], token));
        Assert.True(File.Exists(store.PathOf(audioPath, PixelSide)));
    }

    [Fact]
    public void RemoveOrphanedThumbnails_MissingDirectory_DoesNothing()
    {
        store.RemoveOrphanedThumbnails([], CancellationToken.None);

        Assert.Empty(logger.Entries);
        Assert.False(Directory.Exists(Path.Combine(directory, "cache")));
    }

    [Fact]
    public void RemoveOrphanedThumbnails_InvalidDirectory_LogsWarning()
    {
        var invalidStore = new ThumbnailStore(Path.Combine(directory, new string('a', 512)), logger);

        invalidStore.RemoveOrphanedThumbnails([], CancellationToken.None);

        Assert.Equal(LogLevel.Warning, Assert.Single(logger.Entries).Level);
    }

    public void Dispose()
    {
        Directory.Delete(directory, true);
    }

    private static Bitmap CreateBitmap(PixelSize size)
    {
        using var stream = new MemoryStream(FakeTrackCoverLoader.CreatePng(size));
        return new Bitmap(stream);
    }
}
