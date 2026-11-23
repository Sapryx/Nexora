using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
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
        app.AddLibraryWithCovers(10000);
        var coverCache = app.Services.GetRequiredService<ICoverCache>();
        var gridScroll = app.LibraryGridScroll();
        app.WaitForVisibleCovers<TrackTile>(gridScroll);
        long gridBudget = coverCache.MemoryBudget;
        int visibleTiles = app.VisibleCoverImages<TrackTile>(gridScroll).Count;

        Assert.Equal(CoverMemoryBudget.Screens * TileBytes * GridCoverCount(app) + QueueBudget(app), gridBudget);
        Assert.InRange(gridBudget, CoverMemoryBudget.Screens * TileBytes * visibleTiles, 10000 * TileBytes / 20);

        ScrollThrough<TrackTile>(app, gridScroll, coverCache, LargeLibraryStride);
        Assert.Equal(gridBudget, coverCache.MemoryBudget);

        AssertScrollBackShowsCachedCovers<TrackTile>(app, gridScroll);

        app.Window.Width = 1920;
        app.Window.Height = 1200;
        Dispatcher.UIThread.RunJobs();
        app.WaitForVisibleCovers<TrackTile>(gridScroll);
        Assert.True(coverCache.MemoryBudget > gridBudget);
        Assert.Equal(
            CoverMemoryBudget.Screens * TileBytes * GridCoverCount(app) + QueueBudget(app),
            coverCache.MemoryBudget
        );
        Assert.True(coverCache.MemoryUsed <= coverCache.MemoryBudget);

        app.ShowLibraryList();
        var listScroll = app.LibraryListScroll();
        app.WaitForVisibleCovers<TrackControl>(listScroll);
        int listCovers = SmoothScroller.GetVisibleItemCount(listScroll.Viewport.Height, ItemHeight(listScroll));
        Assert.Equal(
            CoverMemoryBudget.Screens * ThumbnailBytes * listCovers + QueueBudget(app),
            coverCache.MemoryBudget
        );
        Assert.True(coverCache.MemoryUsed <= coverCache.MemoryBudget);

        ScrollThrough<TrackControl>(app, listScroll, coverCache, 1, 40);
        AssertScrollBackShowsCachedCovers<TrackControl>(app, listScroll);

        app.ShowLibraryGrid();
        app.WaitForVisibleCovers<TrackTile>(gridScroll);
        Assert.Equal(
            CoverMemoryBudget.Screens * TileBytes * GridCoverCount(app) + QueueBudget(app),
            coverCache.MemoryBudget
        );
    }

    [AvaloniaFact]
    public void Grid_SmallLibrary_KeepsEveryCoverCached()
    {
        using var app = new TestMainWindow();
        var tracks = app.AddLibraryWithCovers(100);
        var coverCache = app.Services.GetRequiredService<ICoverCache>();
        var gridScroll = app.LibraryGridScroll();
        app.WaitForVisibleCovers<TrackTile>(gridScroll);

        ScrollThrough<TrackTile>(app, gridScroll, coverCache, 1);
        app.ScrollTo(gridScroll, 0);
        app.WaitForVisibleCovers<TrackTile>(gridScroll);

        Assert.All(tracks, track => Assert.Equal(1, app.CoverLoader.LoadCount(track.AudioPath)));
        Assert.Equal(100 * TileBytes, coverCache.MemoryUsed);
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

        for(int step = 1; step <= maxSteps && scrollViewer.Offset.Y < maxOffset; step++)
        {
            app.ScrollByScreens(scrollViewer, screensPerStep);
            app.WaitForVisibleCovers<T>(scrollViewer);
            Assert.True(coverCache.MemoryUsed <= coverCache.MemoryBudget);
        }
    }

    private static void AssertScrollBackShowsCachedCovers<T>(TestMainWindow app, ScrollViewer scrollViewer)
        where T : Control
    {
        app.ScrollByScreens(scrollViewer, -1);
        app.WaitForVisibleCovers<T>(scrollViewer);
        var loadCounts = VisibleLoadCounts<T>(app, scrollViewer);
        app.ScrollByScreens(scrollViewer, 1);
        app.WaitForVisibleCovers<T>(scrollViewer);

        app.ScrollByScreens(scrollViewer, -1);
        var images = app.VisibleCoverImages<T>(scrollViewer);

        Assert.NotEmpty(images);
        Assert.All(images, image => Assert.NotNull(image.Source));
        Assert.Equal(loadCounts, VisibleLoadCounts<T>(app, scrollViewer));
    }

    private static Dictionary<string, int> VisibleLoadCounts<T>(TestMainWindow app, ScrollViewer scrollViewer)
        where T : Control
    {
        return app.VisibleTrackPaths<T>(scrollViewer).ToDictionary(path => path, app.CoverLoader.LoadCount);
    }

    private static long GridCoverCount(TestMainWindow app)
    {
        var grid = app.Find<TrackGrid>("LibraryGrid");
        var gridScroll = app.LibraryGridScroll();
        return SmoothScroller.GetVisibleItemCount(gridScroll.Viewport.Height, ItemHeight(gridScroll))
            * grid.ColumnCount;
    }

    private static long QueueBudget(TestMainWindow app)
    {
        var queueScroll = app.TrackAreaScroll(app.Window.GetVisualDescendants().OfType<TrackArea>()
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
}
