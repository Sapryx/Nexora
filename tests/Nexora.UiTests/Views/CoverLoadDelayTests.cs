using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Core.Tracks;
using Microsoft.Extensions.DependencyInjection;
using Nexora.Controls;
using Nexora.Media;
using Nexora.ViewModels;
using Nexora.Views;
using Tests.Shared.Playback;

namespace Nexora.UiTests.Views;

[Trait("Category", "Slow")]
[Trait("Covers", "src/Nexora/Media")]
[Trait("Covers", "src/Nexora/Controls/SmoothScroller.cs")]
[Trait("Covers", "src/Nexora/ViewModels/CoveredTrackVm.cs")]
[Trait("Covers", "src/Nexora/ViewModels/TrackControlVm.cs")]
[Trait("Covers", "src/Nexora/ViewModels/TrackViewVm.cs")]
[Trait("Covers", "src/Nexora/ViewModels/VisibleCovers.cs")]
[Trait("Covers", "src/Nexora/Views/TrackArea.axaml.cs")]
[Trait("Covers", "src/Nexora/Views/TrackGrid.axaml.cs")]
public class CoverLoadDelayTests
{
    private const int LibrarySize = 1000;
    private const int FastScrollScreens = 2;
    private static readonly TimeSpan FastScrollStep = TimeSpan.FromMilliseconds(20);

    [AvaloniaFact]
    public void Grid_FastScroll_LoadsOnlyCoversShownAfterScrollingStops()
    {
        using var app = new TestMainWindow();
        var tracks = app.AddLibraryWithCovers(LibrarySize);
        app.ShowLibraryGrid();

        AssertFastScrollLoadsOnlyFinalCovers<TrackTile>(
            app,
            app.LibraryGridScroll(),
            tracks,
            app.Find<TrackGrid>().ColumnCount
        );
    }

    [AvaloniaFact]
    public void List_FastScroll_LoadsOnlyCoversShownAfterScrollingStops()
    {
        using var app = new TestMainWindow();
        var tracks = app.AddLibraryWithCovers(LibrarySize);
        app.ShowLibraryList();

        AssertFastScrollLoadsOnlyFinalCovers<TrackControl>(app, app.LibraryListScroll(), tracks, 1);
    }

    [AvaloniaFact]
    public void Grid_ScrollWhileLoading_CancelsHiddenLoadsAndLoadsNewestFirst()
    {
        using var app = new TestMainWindow();
        var tracks = app.AddLibraryWithCovers(LibrarySize);
        app.ShowLibraryGrid();

        AssertLoadsFollowScrolling<TrackTile>(
            app,
            app.LibraryGridScroll(),
            CoverSizes.Tile,
            tracks,
            app.Find<TrackGrid>().ColumnCount
        );
    }

    [AvaloniaFact]
    public void List_ScrollWhileLoading_CancelsHiddenLoadsAndLoadsNewestFirst()
    {
        using var app = new TestMainWindow();
        var tracks = app.AddLibraryWithCovers(LibrarySize);
        app.ShowLibraryList();

        AssertLoadsFollowScrolling<TrackControl>(app, app.LibraryListScroll(), CoverSizes.Thumbnail, tracks, 1);
    }

    [AvaloniaFact]
    public void Player_CoverNotLoaded_LoadsWithoutShowDelay()
    {
        using var app = new TestMainWindow(new FakeMediaEngine());
        var tracks = app.AddLibraryWithCovers(LibrarySize);
        app.ShowLibraryGrid();

        Assert.IsType<TrackControlVm>(app.FindTrack<TrackTile>(tracks[0].AudioPath).DataContext).PressPlayButton();
        var playerImage = app.Find<TrackView>().GetVisualDescendants().OfType<Image>().Single();

        app.WaitUntil(() => playerImage.Source != null);
        Assert.Empty(app.CoverLoader.LoadOrder.Skip(1));
    }

    private static void AssertFastScrollLoadsOnlyFinalCovers<T>(
        TestMainWindow app,
        ScrollViewer scrollViewer,
        List<AudioTrack> tracks,
        int columnCount) where T : Control
    {
        var flashedBy = new HashSet<string>();
        double maxOffset = scrollViewer.Extent.Height - scrollViewer.Viewport.Height;

        while(scrollViewer.Offset.Y < maxOffset)
        {
            flashedBy.UnionWith(app.VisibleTrackPaths<T>(scrollViewer));
            app.ScrollByScreens(scrollViewer, FastScrollScreens);
            app.TimeProvider.Advance(FastScrollStep);
            Dispatcher.UIThread.RunJobs();
        }

        var finalPaths = app.VisibleTrackPaths<T>(scrollViewer);
        flashedBy.ExceptWith(finalPaths);
        flashedBy.ExceptWith(TestMainWindow.NearbyRowPaths(tracks, finalPaths, columnCount));
        Assert.True(flashedBy.Count > LibrarySize / (FastScrollScreens + 1));
        app.TimeProvider.Advance(app.Services.GetRequiredService<CoverLoadQueue>().ShowDelay - FastScrollStep);
        Dispatcher.UIThread.RunJobs();
        Assert.Empty(app.CoverLoader.LoadOrder);
        Assert.All(app.VisibleCoverImages<T>(scrollViewer), image => Assert.Null(image.Source));

        app.TimeProvider.Advance(FastScrollStep);
        Dispatcher.UIThread.RunJobs();
        app.WaitUntil(() => app.VisibleCoverImages<T>(scrollViewer).All(image => image.Source != null));

        Assert.All(flashedBy, path => Assert.Equal(0, app.CoverLoader.LoadCount(path)));
        Assert.All(finalPaths, path => Assert.Equal(1, app.CoverLoader.LoadCount(path)));
    }

    private static void AssertLoadsFollowScrolling<T>(
        TestMainWindow app,
        ScrollViewer scrollViewer,
        double size,
        List<AudioTrack> tracks,
        int columnCount) where T : Control
    {
        var coverCache = app.Services.GetRequiredService<ICoverCache>();
        int maxConcurrentLoads = app.Services.GetRequiredService<CoverLoadQueue>().MaxConcurrentLoads;
        app.CoverLoader.Hold();

        var firstScreen = app.VisibleTrackPaths<T>(scrollViewer);
        app.PassCoverShowDelay();
        app.WaitUntil(() => app.CoverLoader.LoadOrder.Count == maxConcurrentLoads);
        var cancelledLoads = app.CoverLoader.LoadOrder;

        app.ScrollByScreens(scrollViewer, 2);
        Assert.NotNull(app.Window.CaptureRenderedFrame());
        Assert.NotEmpty(app.VisibleCoverImages<T>(scrollViewer));
        Assert.Empty(firstScreen.Intersect(app.VisibleTrackPaths<T>(scrollViewer)));
        app.CoverLoader.Release();
        app.WaitForVisibleCovers<T>(scrollViewer);
        double loadedScreen = scrollViewer.Offset.Y;
        var loadedPaths = app.VisibleTrackPaths<T>(scrollViewer);
        var prefetchedPaths = TestMainWindow.NearbyRowPaths(tracks, loadedPaths, columnCount);
        app.WaitUntil(() => prefetchedPaths.All(path => coverCache.Get(path, size) != null));

        Assert.All(cancelledLoads, path => Assert.Null(coverCache.Get(path, size)));
        Assert.All(
            firstScreen.Except(cancelledLoads),
            path => Assert.Equal(0, app.CoverLoader.LoadCount(path))
        );

        app.CoverLoader.Hold();
        app.ScrollByScreens(scrollViewer, 1);
        var olderPaths = app.VisibleTrackPaths<T>(scrollViewer);
        int loadsBefore = app.CoverLoader.LoadOrder.Count;
        app.PassCoverShowDelay();
        app.WaitUntil(() => app.CoverLoader.LoadOrder.Count == loadsBefore + maxConcurrentLoads);
        var startedOlder = app.CoverLoader.LoadOrder.Skip(loadsBefore).ToList();

        app.ScrollByScreens(scrollViewer, 0.5);
        var visiblePaths = app.VisibleTrackPaths<T>(scrollViewer);
        var newerPaths = visiblePaths.Except(olderPaths).Where(path => coverCache.Get(path, size) == null).ToList();
        var waitingOlder = visiblePaths
            .Intersect(olderPaths)
            .Except(startedOlder)
            .Where(path => coverCache.Get(path, size) == null)
            .ToList();
        Assert.NotEmpty(newerPaths);
        Assert.NotEmpty(waitingOlder);
        app.PassCoverShowDelay();

        while(!newerPaths.All(app.CoverLoader.LoadOrder.Contains))
        {
            int loadCount = app.CoverLoader.LoadOrder.Count;
            app.CoverLoader.ReleaseOne();
            app.WaitUntil(() => app.CoverLoader.LoadOrder.Count > loadCount);
        }

        Assert.Empty(waitingOlder.Intersect(app.CoverLoader.LoadOrder));
        app.CoverLoader.Release();
        app.WaitForVisibleCovers<T>(scrollViewer);

        app.ScrollTo(scrollViewer, loadedScreen);
        Assert.All(app.VisibleCoverImages<T>(scrollViewer), image => Assert.NotNull(image.Source));
    }
}
