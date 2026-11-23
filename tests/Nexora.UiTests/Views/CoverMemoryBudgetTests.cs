using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Core.Library;
using Core.Tracks;
using Microsoft.Extensions.DependencyInjection;
using Nexora.Controls;
using Nexora.Media;
using Nexora.ViewModels;
using Nexora.Views;

namespace Nexora.UiTests.Views;

[Trait("Category", "Slow")]
[Trait("Covers", "src/Core/Collections/LruCache.cs")]
[Trait("Covers", "src/Nexora/Media")]
[Trait("Covers", "src/Nexora/Controls/SmoothScroller.cs")]
[Trait("Covers", "src/Nexora/Views/TrackArea.axaml.cs")]
[Trait("Covers", "src/Nexora/Views/TrackGrid.axaml.cs")]
[Trait("Covers", "src/Nexora/Views/MainWindow.axaml")]
public class CoverMemoryBudgetTests
{
    private const long TileBytes = 160 * 160 * 4;
    private const long ThumbnailBytes = 64 * 64 * 4;
    private const int LargeLibraryStride = 25;

    [AvaloniaFact]
    public void Grid_LargeLibrary_KeepsCoverMemoryWithinViewportBudget()
    {
        using var app = new TestMainWindow();
        AddLibrary(app, 10000);
        var coverCache = app.Services.GetRequiredService<ICoverCache>();
        var gridScroll = app.Find<ScrollViewer>("RowScrollViewer");
        WaitForVisibleCovers<TrackTile>(app, gridScroll);
        long gridBudget = coverCache.MemoryBudget;
        int visibleTiles = VisibleCoverImages<TrackTile>(app, gridScroll).Count;

        Assert.Equal(CoverMemoryBudget.Screens * TileBytes * GridCoverCount(app) + QueueBudget(app), gridBudget);
        Assert.InRange(gridBudget, CoverMemoryBudget.Screens * TileBytes * visibleTiles, 10000 * TileBytes / 20);

        ScrollThrough<TrackTile>(app, gridScroll, coverCache, LargeLibraryStride);
        Assert.Equal(gridBudget, coverCache.MemoryBudget);

        AssertScrollBackShowsCachedCovers<TrackTile>(app, gridScroll);

        app.Window.Width = 1920;
        app.Window.Height = 1200;
        Dispatcher.UIThread.RunJobs();
        WaitForVisibleCovers<TrackTile>(app, gridScroll);
        Assert.True(coverCache.MemoryBudget > gridBudget);
        Assert.Equal(
            CoverMemoryBudget.Screens * TileBytes * GridCoverCount(app) + QueueBudget(app),
            coverCache.MemoryBudget
        );
        Assert.True(coverCache.MemoryUsed <= coverCache.MemoryBudget);

        ShowList(app);
        var listScroll = LibraryListScroll(app);
        WaitForVisibleCovers<TrackControl>(app, listScroll);
        int listCovers = SmoothScroller.GetVisibleItemCount(listScroll.Viewport.Height, ItemHeight(listScroll));
        Assert.Equal(
            CoverMemoryBudget.Screens * ThumbnailBytes * listCovers + QueueBudget(app),
            coverCache.MemoryBudget
        );
        Assert.True(coverCache.MemoryUsed <= coverCache.MemoryBudget);

        ScrollThrough<TrackControl>(app, listScroll, coverCache, 1, 40);
        AssertScrollBackShowsCachedCovers<TrackControl>(app, listScroll);

        ShowGrid(app);
        WaitForVisibleCovers<TrackTile>(app, gridScroll);
        Assert.Equal(
            CoverMemoryBudget.Screens * TileBytes * GridCoverCount(app) + QueueBudget(app),
            coverCache.MemoryBudget
        );
    }

    [AvaloniaFact]
    public void Grid_SmallLibrary_KeepsEveryCoverCached()
    {
        using var app = new TestMainWindow();
        var tracks = AddLibrary(app, 100);
        var coverCache = app.Services.GetRequiredService<ICoverCache>();
        var gridScroll = app.Find<ScrollViewer>("RowScrollViewer");
        WaitForVisibleCovers<TrackTile>(app, gridScroll);

        ScrollThrough<TrackTile>(app, gridScroll, coverCache, 1);
        ScrollTo(gridScroll, 0);
        WaitForVisibleCovers<TrackTile>(app, gridScroll);

        Assert.All(tracks, track => Assert.Equal(1, app.CoverLoader.LoadCount(track.AudioPath)));
        Assert.Equal(100 * TileBytes, coverCache.MemoryUsed);
    }

    private static List<AudioTrack> AddLibrary(TestMainWindow app, int count)
    {
        app.CoverLoader.AddToEveryTrack(new PixelSize(200, 200));
        var tracks = Enumerable.Range(0, count)
            .Select(index => new AudioTrack(
                $"/music/Track {index:D5}.mp3",
                new Metadata() { Title = $"Track {index:D5}", Duration = TimeSpan.FromSeconds(100) }
            ))
            .ToList();
        app.Services.GetRequiredService<MusicLibrary>().Playlist.AddTracks(tracks);
        Dispatcher.UIThread.RunJobs();
        return tracks;
    }

    private static void ScrollThrough<T>(
        TestMainWindow app,
        ScrollViewer scrollViewer,
        ICoverCache coverCache,
        int screensPerStep,
        int maxSteps = int.MaxValue)
        where T : Control
    {
        double maxOffset = scrollViewer.Extent.Height - scrollViewer.Viewport.Height;
        double start = scrollViewer.Offset.Y;

        for(int step = 1; step <= maxSteps && scrollViewer.Offset.Y < maxOffset; step++)
        {
            double offset = start + step * screensPerStep * scrollViewer.Viewport.Height;
            ScrollTo(scrollViewer, Math.Min(maxOffset, offset));
            WaitForVisibleCovers<T>(app, scrollViewer);
            Assert.True(coverCache.MemoryUsed <= coverCache.MemoryBudget);
        }
    }

    private static void AssertScrollBackShowsCachedCovers<T>(TestMainWindow app, ScrollViewer scrollViewer)
        where T : Control
    {
        double currentScreen = scrollViewer.Offset.Y;
        double previousScreen = currentScreen - scrollViewer.Viewport.Height;
        ScrollTo(scrollViewer, previousScreen);
        WaitForVisibleCovers<T>(app, scrollViewer);
        var loadCounts = VisibleLoadCounts<T>(app, scrollViewer);
        ScrollTo(scrollViewer, currentScreen);
        WaitForVisibleCovers<T>(app, scrollViewer);

        ScrollTo(scrollViewer, previousScreen);
        var images = VisibleCoverImages<T>(app, scrollViewer);

        Assert.NotEmpty(images);
        Assert.All(images, image => Assert.NotNull(image.Source));
        Assert.Equal(loadCounts, VisibleLoadCounts<T>(app, scrollViewer));
    }

    private static Dictionary<string, int> VisibleLoadCounts<T>(TestMainWindow app, ScrollViewer scrollViewer)
        where T : Control
    {
        return VisibleCoverImages<T>(app, scrollViewer)
            .Select(image => Assert.IsType<TrackControlVm>(image.DataContext).AudioPath)
            .ToDictionary(path => path, app.CoverLoader.LoadCount);
    }

    private static void ScrollTo(ScrollViewer scrollViewer, double offset)
    {
        scrollViewer.Offset = new Vector(0, offset);
        Dispatcher.UIThread.RunJobs();
    }

    private static void WaitForVisibleCovers<T>(TestMainWindow app, ScrollViewer scrollViewer) where T : Control
    {
        app.WaitUntil(() =>
        {
            var images = VisibleCoverImages<T>(app, scrollViewer);
            return images.Count > 0 && images.All(image => image.Source != null);
        });
    }

    private static List<Image> VisibleCoverImages<T>(TestMainWindow app, ScrollViewer scrollViewer) where T : Control
    {
        return scrollViewer.GetVisualDescendants().OfType<T>()
            .Where(track => track.IsEffectivelyVisible && IsInViewport(track, scrollViewer))
            .Select(track => track.GetVisualDescendants().OfType<Image>().First())
            .ToList();
    }

    private static bool IsInViewport(Control control, ScrollViewer scrollViewer)
    {
        var top = control.TranslatePoint(new Point(0, 0), scrollViewer);
        return top != null
            && top.Value.Y < scrollViewer.Viewport.Height
            && top.Value.Y + control.Bounds.Height > 0;
    }

    private static long GridCoverCount(TestMainWindow app)
    {
        var grid = app.Find<TrackGrid>("LibraryGrid");
        var gridScroll = app.Find<ScrollViewer>("RowScrollViewer");
        return SmoothScroller.GetVisibleItemCount(gridScroll.Viewport.Height, ItemHeight(gridScroll))
            * grid.ColumnCount;
    }

    private static long QueueBudget(TestMainWindow app)
    {
        var queueScroll = TrackAreaScroll(app.Window.GetVisualDescendants().OfType<TrackArea>()
            .Single(area => area.Name != "LibraryList"));
        int covers = SmoothScroller.GetVisibleItemCount(queueScroll.Viewport.Height, ItemHeight(queueScroll));
        return CoverMemoryBudget.Screens * ThumbnailBytes * covers;
    }

    private static double ItemHeight(ScrollViewer scrollViewer)
    {
        var itemsControl = scrollViewer.FindAncestorOfType<ListBox>()
            ?? scrollViewer.GetVisualDescendants().OfType<ItemsControl>().First();
        var container = itemsControl.GetRealizedContainers().FirstOrDefault(it => it.Bounds.Height > 0);
        return container?.Bounds.Height ?? 100;
    }

    private static ScrollViewer LibraryListScroll(TestMainWindow app)
    {
        return TrackAreaScroll(app.Find<TrackArea>("LibraryList"));
    }

    private static ScrollViewer TrackAreaScroll(TrackArea trackArea)
    {
        return trackArea.GetVisualDescendants().OfType<ScrollViewer>().First();
    }

    private static void ShowList(TestMainWindow app)
    {
        app.Find<SegmentedControl>("LibraryViewSwitch").SelectedIndex = 1;
        Dispatcher.UIThread.RunJobs();
    }

    private static void ShowGrid(TestMainWindow app)
    {
        app.Find<SegmentedControl>("LibraryViewSwitch").SelectedIndex = 0;
        Dispatcher.UIThread.RunJobs();
    }
}
