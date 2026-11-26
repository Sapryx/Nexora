using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Core.Tracks;
using Microsoft.Extensions.DependencyInjection;
using Nexora.Controls;
using Nexora.Media;
using Nexora.Views;

namespace Nexora.UiTests.Views;

[Trait("Category", "Slow")]
[Trait("Covers", "src/Nexora/Media")]
[Trait("Covers", "src/Nexora/Controls/SmoothScroller.cs")]
[Trait("Covers", "src/Nexora/ViewModels/TrackControlVm.cs")]
[Trait("Covers", "src/Nexora/ViewModels/VisibleCovers.cs")]
[Trait("Covers", "src/Nexora/Views/TrackArea.axaml.cs")]
[Trait("Covers", "src/Nexora/Views/TrackGrid.axaml.cs")]
public class CoverPrefetchTests
{
    private const int LibrarySize = 1000;
    private static readonly TimeSpan FastScrollStep = TimeSpan.FromMilliseconds(20);

    [AvaloniaFact]
    public void Grid_ScrollingSettles_PrefetchesRowsAboveAndBelowAfterVisibleOnes()
    {
        using var app = new TestMainWindow();
        var tracks = app.AddLibraryWithCovers(LibrarySize);
        app.ShowLibraryGrid();

        new PrefetchCheck<TrackTile>(app, app.LibraryGridScroll(), tracks, app.Find<TrackGrid>().ColumnCount).Run();
    }

    [AvaloniaFact]
    public void List_ScrollingSettles_PrefetchesRowsAboveAndBelowAfterVisibleOnes()
    {
        using var app = new TestMainWindow();
        var tracks = app.AddLibraryWithCovers(LibrarySize);
        app.ShowLibraryList();

        new PrefetchCheck<TrackControl>(app, app.LibraryListScroll(), tracks, 1).Run();
    }

    private sealed class PrefetchCheck<T> where T : Control
    {
        private readonly TestMainWindow app;
        private readonly ScrollViewer scrollViewer;
        private readonly List<AudioTrack> tracks;
        private readonly int columnCount;
        private readonly ICoverCache coverCache;
        private readonly double rowStep;
        private readonly double coverSize;

        public PrefetchCheck(TestMainWindow app, ScrollViewer scrollViewer, List<AudioTrack> tracks, int columnCount)
        {
            this.app = app;
            this.scrollViewer = scrollViewer;
            this.tracks = tracks;
            this.columnCount = columnCount;
            coverCache = app.Services.GetRequiredService<ICoverCache>();
            rowStep = TopOf(tracks[columnCount].AudioPath) - TopOf(tracks[0].AudioPath);
            coverSize = columnCount > 1 ? CoverSizes.Tile : CoverSizes.Thumbnail;
        }

        public void Run()
        {
            int rowCount = LibrarySize / columnCount;
            int middleRow = rowCount / 2;
            app.ScrollTo(scrollViewer, MiddleOfRow(middleRow));
            AssertSettledLoadsVisibleThenNearbyRows(middleRow);

            app.ScrollTo(scrollViewer, MiddleOfRow(middleRow + 1));
            Assert.All(app.VisibleCoverImages<T>(scrollViewer), image => Assert.NotNull(image.Source));
            Assert.All(
                app.VisibleTrackPaths<T>(scrollViewer),
                path => Assert.Equal(1, app.CoverLoader.LoadCount(path))
            );

            int targetRow = rowCount / 8;
            int loadsBefore = app.CoverLoader.LoadOrder.Count;
            FastScrollTo(MiddleOfRow(targetRow));
            Assert.Equal(loadsBefore, app.CoverLoader.LoadOrder.Count);
            AssertSettledLoadsVisibleThenNearbyRows(targetRow);
        }

        private void AssertSettledLoadsVisibleThenNearbyRows(int firstRow)
        {
            int loadsBefore = app.CoverLoader.LoadOrder.Count;
            var visiblePaths = app.VisibleTrackPaths<T>(scrollViewer);
            int lastRow = firstRow + visiblePaths.Count / columnCount - 1;
            Assert.Equal(PathsOfRows(firstRow, lastRow), visiblePaths.Order());
            var nearbyPaths = TestMainWindow.NearbyRowPaths(tracks, visiblePaths, columnCount);
            Assert.Equal(2 * columnCount, nearbyPaths.Count);

            app.PassCoverShowDelay();
            app.WaitUntil(() => app.CoverLoader.LoadOrder.Count == loadsBefore + visiblePaths.Count + 2 * columnCount);
            Dispatcher.UIThread.RunJobs();
            var loads = app.CoverLoader.LoadOrder.Skip(loadsBefore).ToList();

            Assert.Equal(visiblePaths.Order(), loads.Take(visiblePaths.Count).Order());
            Assert.Equal(nearbyPaths.Order(), loads.Skip(visiblePaths.Count).Order());
            app.WaitUntil(() => loads.All(path => coverCache.Get(path, coverSize) != null));
            app.TimeProvider.Advance(TimeSpan.FromSeconds(1));
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(loads.Count, app.CoverLoader.LoadOrder.Count - loadsBefore);
            Assert.InRange(coverCache.MemoryUsed, 1, coverCache.MemoryBudget);
        }

        private void FastScrollTo(double target)
        {
            while(Math.Abs(scrollViewer.Offset.Y - target) > scrollViewer.Viewport.Height)
            {
                app.ScrollByScreens(scrollViewer, Math.Sign(target - scrollViewer.Offset.Y) * 2);
                app.TimeProvider.Advance(FastScrollStep);
                Dispatcher.UIThread.RunJobs();
            }

            app.ScrollTo(scrollViewer, target);
        }

        private double MiddleOfRow(int row)
        {
            return row * rowStep + rowStep / 2;
        }

        private List<string> PathsOfRows(int firstRow, int lastRow)
        {
            return tracks
                .Skip(firstRow * columnCount)
                .Take((lastRow - firstRow + 1) * columnCount)
                .Select(track => track.AudioPath)
                .Order()
                .ToList();
        }

        private double TopOf(string audioPath)
        {
            return app.FindTrack<T>(audioPath).TranslatePoint(new Point(0, 0), scrollViewer)?.Y
                ?? throw new InvalidOperationException($"{audioPath} is not in the scroll viewer");
        }
    }
}
