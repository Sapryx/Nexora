using Microsoft.Extensions.Time.Testing;
using Nexora.Media;
using Nexora.Tests.Media;
using Nexora.ViewModels;

namespace Nexora.Tests.ViewModels;

public class VisibleCoversTests
{
    private readonly FakeTimeProvider timeProvider = new FakeTimeProvider();
    private readonly CoverLoads coverLoads = new CoverLoads();
    private readonly VisibleCovers visibleCovers = new VisibleCovers(CoverSizes.Tile);
    private readonly CoverLoadQueue coverLoadQueue;

    public VisibleCoversTests()
    {
        coverLoadQueue = TestCoverLoadQueue.Create(coverLoads.CoverCacheMock.Object, timeProvider);
    }

    [Fact]
    public void Show_NewTracks_LoadsTheirCoversAtCoverSize()
    {
        Show([CreateTrackVm("A"), CreateTrackVm("B")], 0, 2);
        timeProvider.Advance(TestCoverLoadQueue.ShowDelay);

        Assert.Equal(["A.mp3", "B.mp3"], coverLoads.StartedPaths.Order());
        Assert.All(coverLoads.Started, load => Assert.Equal(CoverSizes.Tile, load.Size));
    }

    [Fact]
    public void Show_TrackNoLongerVisible_CancelsItsLoadAndKeepsTheOthers()
    {
        var first = CreateTrackVm("A");
        var kept = CreateTrackVm("B");
        Show([first, kept], 0, 2);
        timeProvider.Advance(TestCoverLoadQueue.ShowDelay);

        Show([kept, CreateTrackVm("C")], 0, 2);
        timeProvider.Advance(TestCoverLoadQueue.ShowDelay);

        Assert.True(coverLoads.Of("A.mp3").Token.IsCancellationRequested);
        Assert.False(coverLoads.Of("B.mp3").Token.IsCancellationRequested);
        Assert.Equal(["A.mp3", "B.mp3", "C.mp3"], coverLoads.StartedPaths.Order());
    }

    [Fact]
    public void Show_NoVisibleRows_CancelsEveryLoadAndPrefetchesNothing()
    {
        var trackVms = CreateTrackVms("A", "B", "C");
        Show(trackVms, 1, 1);

        Show(trackVms, 1, 0);
        timeProvider.Advance(TestCoverLoadQueue.ShowDelay);

        Assert.Empty(coverLoads.Started);
    }

    [Fact]
    public void Show_RowsAroundVisibleOnes_PrefetchesOneRowOnEachSideAfterVisibleOnes()
    {
        var rows = new List<TrackControlVm[]>
        {
            CreateTrackVms("A1", "A2"),
            CreateTrackVms("B1", "B2"),
            CreateTrackVms("C1", "C2"),
            CreateTrackVms("D1", "D2"),
            CreateTrackVms("E1", "E2")
        };

        visibleCovers.Show(rows, 2, 1, row => row);
        timeProvider.Advance(TestCoverLoadQueue.ShowDelay);
        coverLoads.Of("C1.mp3").Completion.SetResult(null);
        coverLoads.Of("C2.mp3").Completion.SetResult(null);

        Assert.True(SpinWait.SpinUntil(() => coverLoads.Started.Count == 6, TimeSpan.FromSeconds(5)));
        Assert.Equal(["C1.mp3", "C2.mp3"], coverLoads.StartedPaths.Take(2).Order());
        Assert.Equal(["B1.mp3", "B2.mp3", "D1.mp3", "D2.mp3"], coverLoads.StartedPaths.Skip(2).Order());
    }

    [Fact]
    public void Show_FirstRowVisible_PrefetchesOnlyTheRowBelow()
    {
        Show(CreateTrackVms("A", "B", "C"), 0, 1);
        timeProvider.Advance(TestCoverLoadQueue.ShowDelay);
        coverLoads.Of("A.mp3").Completion.SetResult(null);

        Assert.True(SpinWait.SpinUntil(() => coverLoads.Started.Count == 2, TimeSpan.FromSeconds(5)));
        Assert.Equal(["A.mp3", "B.mp3"], coverLoads.StartedPaths);
    }

    [Fact]
    public void Show_PrefetchedRowScrolledOutOfReach_CancelsItsLoad()
    {
        var trackVms = CreateTrackVms("A", "B", "C", "D");
        Show(trackVms, 0, 1);

        Show(trackVms, 2, 1);
        timeProvider.Advance(TestCoverLoadQueue.ShowDelay);

        Assert.Equal(["C.mp3"], coverLoads.StartedPaths);
    }

    private void Show(IReadOnlyList<TrackControlVm> trackVms, int firstIndex, int count)
    {
        visibleCovers.Show(trackVms, firstIndex, count, trackVm => [trackVm]);
    }

    private TrackControlVm[] CreateTrackVms(params string[] titles)
    {
        return titles.Select(CreateTrackVm).ToArray();
    }

    private TrackControlVm CreateTrackVm(string title)
    {
        return TestTrackControlVm.Create(title, coverLoads.CoverCacheMock.Object, coverLoadQueue);
    }
}
