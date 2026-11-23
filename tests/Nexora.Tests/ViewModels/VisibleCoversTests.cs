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
        visibleCovers.Show([CreateTrackVm("A"), CreateTrackVm("B")]);
        timeProvider.Advance(TestCoverLoadQueue.ShowDelay);

        Assert.Equal(["A.mp3", "B.mp3"], coverLoads.StartedPaths.Order());
        Assert.All(coverLoads.Started, load => Assert.Equal(CoverSizes.Tile, load.Size));
    }

    [Fact]
    public void Show_TrackNoLongerVisible_CancelsItsLoadAndKeepsTheOthers()
    {
        var first = CreateTrackVm("A");
        var kept = CreateTrackVm("B");
        visibleCovers.Show([first, kept]);
        timeProvider.Advance(TestCoverLoadQueue.ShowDelay);

        visibleCovers.Show([kept, CreateTrackVm("C")]);
        timeProvider.Advance(TestCoverLoadQueue.ShowDelay);

        Assert.True(coverLoads.Of("A.mp3").Token.IsCancellationRequested);
        Assert.False(coverLoads.Of("B.mp3").Token.IsCancellationRequested);
        Assert.Equal(["A.mp3", "B.mp3", "C.mp3"], coverLoads.StartedPaths.Order());
    }

    [Fact]
    public void Show_NoTracks_CancelsEveryLoad()
    {
        visibleCovers.Show([CreateTrackVm("A"), CreateTrackVm("B")]);

        visibleCovers.Show([]);
        timeProvider.Advance(TestCoverLoadQueue.ShowDelay);

        Assert.Empty(coverLoads.Started);
    }

    private TrackControlVm CreateTrackVm(string title)
    {
        return TestTrackControlVm.Create(title, coverLoads.CoverCacheMock.Object, coverLoadQueue);
    }
}
