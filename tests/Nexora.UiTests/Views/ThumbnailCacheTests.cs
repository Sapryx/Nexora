using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media.Imaging;
using Core.Tracks;
using Microsoft.Extensions.Logging.Abstractions;
using Nexora.Controls;
using Nexora.Media;

namespace Nexora.UiTests.Views;

[Trait("Category", "Slow")]
[Trait("Covers", "src/Nexora/Media/CoverCache.cs")]
[Trait("Covers", "src/Nexora/Media/ThumbnailStore.cs")]
public sealed class ThumbnailCacheTests : IDisposable
{
    private const int LibrarySize = 12;
    private const string RealTrackName = "Metallica - Sad But True";
    private readonly string thumbnailDirectory = TestMainWindow.CreateTemporaryDirectory();
    private readonly string musicDirectory = TestMainWindow.CreateTemporaryDirectory();
    private readonly string realTrackPath;
    private readonly ThumbnailStore thumbnailStore;

    public ThumbnailCacheTests()
    {
        realTrackPath = Path.Combine(musicDirectory, $"{RealTrackName}.mp3");
        File.WriteAllBytes(realTrackPath, [1, 2, 3]);
        File.SetLastWriteTimeUtc(realTrackPath, DateTime.UtcNow.AddDays(-1));
        thumbnailStore = new ThumbnailStore(thumbnailDirectory, NullLogger<ThumbnailStore>.Instance);
    }

    [AvaloniaFact]
    public void Covers_AcrossRestarts_AreKeptAsPngThumbnailsNamedAfterAudioFileAndPixelSize()
    {
        using(var app = Start(1))
        {
            app.ShowLibraryGrid();
            var gridPaths = WaitForVisibleCovers<TrackTile>(app, app.LibraryGridScroll());
            Assert.Equal(new PixelSize(160, 160), ThumbnailSize($"{RealTrackName} 160x160.png"));
            AssertThumbnails(gridPaths, 160);
            AssertOnlyLoadedTracksHaveThumbnails(app);

            app.ShowLibraryList();
            var listPaths = WaitForVisibleCovers<TrackControl>(app, app.LibraryListScroll());
            Assert.Equal(new PixelSize(64, 64), ThumbnailSize($"{RealTrackName} 64x64.png"));
            AssertThumbnails(listPaths, 64);
        }

        using(var app = Start(1.5))
        {
            app.ShowLibraryGrid();
            AssertThumbnails(WaitForVisibleCovers<TrackTile>(app, app.LibraryGridScroll()), 240);
            app.ShowLibraryList();
            AssertThumbnails(WaitForVisibleCovers<TrackControl>(app, app.LibraryListScroll()), 96);
            Assert.Equal(new PixelSize(240, 240), ThumbnailSize($"{RealTrackName} 240x240.png"));
            Assert.Equal(new PixelSize(96, 96), ThumbnailSize($"{RealTrackName} 96x96.png"));
            Assert.True(File.Exists(Path.Combine(thumbnailDirectory, $"{RealTrackName} 160x160.png")));
            Assert.True(File.Exists(Path.Combine(thumbnailDirectory, $"{RealTrackName} 64x64.png")));
        }

        using(var app = Start(1))
        {
            app.ShowLibraryGrid();
            var gridPaths = WaitForVisibleCovers<TrackTile>(app, app.LibraryGridScroll());
            app.ShowLibraryList();
            var listPaths = WaitForVisibleCovers<TrackControl>(app, app.LibraryListScroll());
            Assert.All(gridPaths.Concat(listPaths), path => Assert.Equal(0, app.CoverLoader.LoadCount(path)));
        }

        File.SetLastWriteTimeUtc(realTrackPath, DateTime.UtcNow);

        using(var app = Start(1, new PixelSize(300, 150)))
        {
            app.ShowLibraryGrid();
            var gridPaths = WaitForVisibleCovers<TrackTile>(app, app.LibraryGridScroll());
            Assert.Equal(1, app.CoverLoader.LoadCount(realTrackPath));
            Assert.All(
                gridPaths.Where(path => path != realTrackPath),
                path => Assert.Equal(0, app.CoverLoader.LoadCount(path))
            );
            Assert.Equal(new PixelSize(160, 80), ThumbnailSize($"{RealTrackName} 160x160.png"));
            app.ShowLibraryList();
            WaitForVisibleCovers<TrackControl>(app, app.LibraryListScroll());
            Assert.Equal(2, app.CoverLoader.LoadCount(realTrackPath));
            Assert.Equal(new PixelSize(64, 32), ThumbnailSize($"{RealTrackName} 64x64.png"));
        }

        Directory.Delete(thumbnailDirectory, true);

        using(var app = Start(1))
        {
            app.ShowLibraryGrid();
            var gridPaths = WaitForVisibleCovers<TrackTile>(app, app.LibraryGridScroll());
            Assert.All(gridPaths, path => Assert.Equal(1, app.CoverLoader.LoadCount(path)));
            AssertThumbnails(gridPaths, 160);
        }
    }

    [AvaloniaFact]
    public void TrackWithoutCover_AfterRestart_IsNotReadAgain()
    {
        const string noCoverPath = "/music/No Cover.mp3";

        for(int run = 0; run < 2; run++)
        {
            using var app = new TestMainWindow(null, null, thumbnailDirectory);
            app.AddToLibrary([CreateTrack(noCoverPath, "No Cover")]);
            app.ShowLibraryGrid();
            var tile = app.FindTrack<TrackTile>(noCoverPath);
            app.PassCoverShowDelay();
            app.WaitUntil(() => File.Exists(Path.Combine(thumbnailDirectory, "No Cover 160x160.png")));

            Assert.Null(app.CoverImageOf(tile).Source);
            Assert.Equal(run == 0 ? 1 : 0, app.CoverLoader.LoadCount(noCoverPath));
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

    private static AudioTrack CreateTrack(string audioPath, string title)
    {
        return new AudioTrack(audioPath, new Metadata() { Title = title, Duration = TimeSpan.FromSeconds(100) });
    }

    private static List<string> WaitForVisibleCovers<T>(TestMainWindow app, ScrollViewer scrollViewer)
        where T : Control
    {
        app.WaitForVisibleCovers<T>(scrollViewer);
        return app.VisibleTrackPaths<T>(scrollViewer);
    }

    private TestMainWindow Start(
        double renderScaling,
        PixelSize? realTrackCover = null,
        [CallerMemberName] string testName = "")
    {
        var app = new TestMainWindow(null, null, thumbnailDirectory, testName);
        app.RenderScale.RenderScaling = renderScaling;
        app.CoverLoader.Add(realTrackPath, realTrackCover ?? new PixelSize(200, 200));
        app.AddToLibrary([CreateTrack(realTrackPath, RealTrackName)]);
        app.AddLibraryWithCovers(LibrarySize);
        return app;
    }

    private void AssertThumbnails(List<string> audioPaths, int pixelSide)
    {
        Assert.NotEmpty(audioPaths);
        Assert.All(
            audioPaths,
            path => Assert.True(File.Exists(ThumbnailPath(path, pixelSide)), $"No thumbnail for {path}")
        );
    }

    private void AssertOnlyLoadedTracksHaveThumbnails(TestMainWindow app)
    {
        var loadedThumbnails = app.CoverLoader.LoadOrder.Select(path => ThumbnailPath(path, 160)).ToHashSet();
        Assert.All(Directory.EnumerateFiles(thumbnailDirectory), file => Assert.Contains(file, loadedThumbnails));
    }

    private string ThumbnailPath(string audioPath, int pixelSide)
    {
        return thumbnailStore.PathOf(audioPath, pixelSide);
    }

    private PixelSize ThumbnailSize(string fileName)
    {
        using var bitmap = new Bitmap(Path.Combine(thumbnailDirectory, fileName));
        return bitmap.PixelSize;
    }
}
