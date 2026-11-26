using System.Diagnostics;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Core.Tracks;
using Microsoft.Extensions.Logging.Abstractions;
using Nexora.Controls;
using Nexora.Media;
using Tests.Shared.Storage;

namespace Nexora.UiTests.Views;

[Trait("Category", "Slow")]
[Trait("Covers", "src/Nexora/Media/ThumbnailGenerator.cs")]
[Trait("Covers", "src/Nexora/Media/CoverCache.cs")]
[Trait("Covers", "src/Nexora/Media/ThumbnailStore.cs")]
public sealed class BackgroundThumbnailTests : IDisposable
{
    private const int LargeLibrarySize = 10000;
    private const int SmallLibrarySize = 40;
    private const int ClosedLibrarySize = 1000;
    private const string RealTrackName = "Metallica - Sad But True";
    private static readonly TimeSpan GenerationTimeout = TimeSpan.FromMinutes(3);
    private static readonly TimeSpan MaxScrollStep = TimeSpan.FromMilliseconds(500);
    private readonly string thumbnailDirectory = TestMainWindow.CreateTemporaryDirectory();
    private readonly string musicDirectory = TestMainWindow.CreateTemporaryDirectory();
    private readonly string realTrackPath;
    private readonly ThumbnailStore thumbnailStore;

    public BackgroundThumbnailTests()
    {
        realTrackPath = TestWavFile.Create(musicDirectory);
        thumbnailStore = new ThumbnailStore(thumbnailDirectory, NullLogger<ThumbnailStore>.Instance);
    }

    [AvaloniaFact]
    public void EmptyCache_LargeLibrary_CreatesEveryThumbnailInBackgroundWhileGridScrolls()
    {
        using var app = new TestMainWindow(null, null, thumbnailDirectory);
        app.ShowLibraryGrid();
        var tracks = app.CreateTracksWithCovers(LargeLibrarySize);
        Assert.Empty(Directory.GetFiles(thumbnailDirectory));

        var generator = app.LoadLibrary(tracks);
        var scrollViewer = app.LibraryGridScroll();

        foreach(double position in new[] { 0.5, 0.75, 0.9, 0.3, 0.6 })
        {
            Assert.False(generator.Completion.IsCompleted, "Thumbnail generation finished before scrolling");
            double previousOffset = scrollViewer.Offset.Y;
            long started = Stopwatch.GetTimestamp();
            app.ScrollTo(scrollViewer, (scrollViewer.Extent.Height - scrollViewer.Viewport.Height) * position);
            Assert.NotNull(app.Window.CaptureRenderedFrame());
            var elapsed = Stopwatch.GetElapsedTime(started);
            Assert.True(elapsed < MaxScrollStep, $"A scroll step took {elapsed.TotalMilliseconds} ms");
            Assert.NotEqual(previousOffset, scrollViewer.Offset.Y);
            app.WaitForVisibleCovers<TrackTile>(scrollViewer);
        }

        WaitForGenerator(generator);

        Assert.Equal(LargeLibrarySize * 2, Directory.GetFiles(thumbnailDirectory).Length);
        AssertBothThumbnails(tracks.Select(track => track.AudioPath));
        Assert.All(tracks, track => Assert.Equal(1, app.CoverLoader.LoadCount(track.AudioPath)));
    }

    [AvaloniaFact]
    public void Restart_OnlyTracksWithChangedCoversAreRead()
    {
        WriteEmbeddedCover(new PixelSize(200, 200));
        File.SetLastWriteTimeUtc(realTrackPath, DateTime.UtcNow.AddDays(-1));

        using(var app = Start())
        {
            WaitForGenerator(app.LoadLibrary(LibraryOf(app)));
            Assert.Equal(SmallLibrarySize + 1, app.CoverLoader.LoadOrder.Count);
            AssertBothThumbnails(app.CoverLoader.LoadOrder);
        }

        using(var app = Start())
        {
            WaitForGenerator(app.LoadLibrary(LibraryOf(app)));
            app.ShowLibraryGrid();
            app.WaitForVisibleCovers<TrackTile>(app.LibraryGridScroll());
            Assert.Empty(app.CoverLoader.LoadOrder);
        }

        WriteEmbeddedCover(new PixelSize(300, 150));

        using(var app = Start())
        {
            var tracks = LibraryOf(app);
            WaitForGenerator(app.LoadLibrary(tracks));
            Assert.Equal([realTrackPath], app.CoverLoader.LoadOrder);
            Assert.Equal(new PixelSize(160, 80), TestMainWindow.ThumbnailSize(ThumbnailPath(realTrackPath, 160)));
            Assert.Equal(new PixelSize(64, 32), TestMainWindow.ThumbnailSize(ThumbnailPath(realTrackPath, 64)));
            app.ShowLibraryGrid();
            app.WaitForVisibleCovers<TrackTile>(app.LibraryGridScroll());
        }
    }

    [AvaloniaFact]
    public void Close_WhileCreating_LeavesOnlyCompleteThumbnails()
    {
        List<AudioTrack> tracks;
        ThumbnailGenerator generator;

        using(var app = Start())
        {
            tracks = app.CreateTracksWithCovers(ClosedLibrarySize);
            generator = app.LoadLibrary(tracks);
            app.WaitUntil(() => Directory.GetFiles(thumbnailDirectory).Length >= 100);
        }

        string[] files = Directory.GetFiles(thumbnailDirectory);
        Assert.True(generator.Completion.IsCompletedSuccessfully);
        Assert.True(files.Length < ClosedLibrarySize * 2);
        Assert.All(files, file => Assert.True(file.EndsWith(".png") && IsCompleteThumbnail(file), file));
        var createdPaths = tracks
            .Select(track => track.AudioPath)
            .Where(path => File.Exists(ThumbnailPath(path, 160)) && File.Exists(ThumbnailPath(path, 64)))
            .ToHashSet();
        Assert.NotEmpty(createdPaths);

        using(var app = Start())
        {
            WaitForGenerator(app.LoadLibrary(app.CreateTracksWithCovers(ClosedLibrarySize)));
            AssertBothThumbnails(tracks.Select(track => track.AudioPath));
            Assert.All(createdPaths, path => Assert.Equal(0, app.CoverLoader.LoadCount(path)));
        }
    }

    public void Dispose()
    {
        foreach(string directory in new[] { thumbnailDirectory, musicDirectory })
        {
            if(Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }
    }

    private static void WaitForGenerator(ThumbnailGenerator generator)
    {
        Assert.True(generator.Completion.Wait(GenerationTimeout), "Thumbnails were not created in time");
        Dispatcher.UIThread.RunJobs();
    }

    private static bool IsCompleteThumbnail(string file)
    {
        var size = TestMainWindow.ThumbnailSize(file);
        return size == new PixelSize(160, 160) || size == new PixelSize(64, 64);
    }

    private TestMainWindow Start([CallerMemberName] string testName = "")
    {
        return new TestMainWindow(null, null, thumbnailDirectory, testName);
    }

    private List<AudioTrack> LibraryOf(TestMainWindow app)
    {
        app.CoverLoader.FileCoverPath = realTrackPath;
        var realTrack = new AudioTrack(
            realTrackPath,
            new Metadata() { Title = RealTrackName, Duration = TimeSpan.FromSeconds(100) }
        );
        return app.CreateTracksWithCovers(SmallLibrarySize).Prepend(realTrack).ToList();
    }

    private void WriteEmbeddedCover(PixelSize size)
    {
        using var tagFile = TagLib.File.Create(realTrackPath);
        TagLib.ByteVector data = FakeTrackCoverLoader.CreatePng(size);
        tagFile.Tag.Pictures = [new TagLib.Picture(data)];
        tagFile.Save();
    }

    private void AssertBothThumbnails(IEnumerable<string> audioPaths)
    {
        Assert.All(audioPaths, path =>
        {
            Assert.True(File.Exists(ThumbnailPath(path, 160)), $"No grid thumbnail for {path}");
            Assert.True(File.Exists(ThumbnailPath(path, 64)), $"No list thumbnail for {path}");
        });
    }

    private string ThumbnailPath(string audioPath, int pixelSide)
    {
        return thumbnailStore.PathOf(audioPath, pixelSide);
    }
}
