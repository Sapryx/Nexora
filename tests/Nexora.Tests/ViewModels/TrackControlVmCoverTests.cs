using Microsoft.Extensions.Time.Testing;
using Nexora.Media;
using Nexora.Tests.Media;
using Nexora.ViewModels;

namespace Nexora.Tests.ViewModels;

public class TrackControlVmCoverTests
{
    private readonly FakeTimeProvider timeProvider = new FakeTimeProvider();
    private readonly CoverLoads coverLoads = new CoverLoads();
    private readonly CoverLoadQueue coverLoadQueue;
    private readonly TrackControlVm trackVm;

    public TrackControlVmCoverTests()
    {
        coverLoadQueue = TestCoverLoadQueue.Create(coverLoads.CoverCacheMock.Object, timeProvider);
        trackVm = TestTrackControlVm.Create("Psycho", coverLoads.CoverCacheMock.Object, coverLoadQueue);
    }

    [Fact]
    public void ShowCover_BeforeShowDelay_DoesNotLoad()
    {
        trackVm.ShowCover(CoverSizes.Tile, false);
        timeProvider.Advance(TestCoverLoadQueue.ShowDelay - TimeSpan.FromMilliseconds(1));

        Assert.Empty(coverLoads.Started);
    }

    [Fact]
    public void ShowCover_AfterShowDelay_LoadsTrackCoverAtSize()
    {
        trackVm.ShowCover(CoverSizes.Tile, false);
        timeProvider.Advance(TestCoverLoadQueue.ShowDelay);

        var load = Assert.Single(coverLoads.Started);
        Assert.Equal("Psycho.mp3", load.AudioPath);
        Assert.Equal(CoverSizes.Tile, load.Size);
    }

    [Fact]
    public void ShowCover_Twice_LoadsOnce()
    {
        trackVm.ShowCover(CoverSizes.Tile, false);
        trackVm.ShowCover(CoverSizes.Tile, false);
        timeProvider.Advance(TestCoverLoadQueue.ShowDelay);

        Assert.Single(coverLoads.Started);
    }

    [Fact]
    public void ShowCover_BothSizes_LoadsEach()
    {
        trackVm.ShowCover(CoverSizes.Tile, false);
        trackVm.ShowCover(CoverSizes.Thumbnail, false);
        timeProvider.Advance(TestCoverLoadQueue.ShowDelay);

        Assert.Equal(
            [CoverSizes.Thumbnail, CoverSizes.Tile],
            coverLoads.Started.Select(load => load.Size).Order()
        );
    }

    [Fact]
    public void HideCover_BeforeShowDelay_NeverLoads()
    {
        trackVm.ShowCover(CoverSizes.Tile, false);
        trackVm.HideCover(CoverSizes.Tile);
        timeProvider.Advance(TestCoverLoadQueue.ShowDelay);

        Assert.Empty(coverLoads.Started);
    }

    [Fact]
    public void HideCover_WhileLoading_CancelsLoad()
    {
        trackVm.ShowCover(CoverSizes.Tile, false);
        timeProvider.Advance(TestCoverLoadQueue.ShowDelay);

        trackVm.HideCover(CoverSizes.Tile);

        Assert.True(Assert.Single(coverLoads.Started).Token.IsCancellationRequested);
    }

    [Fact]
    public void ShowCover_AfterHide_LoadsAgain()
    {
        trackVm.ShowCover(CoverSizes.Tile, false);
        trackVm.HideCover(CoverSizes.Tile);
        trackVm.ShowCover(CoverSizes.Tile, false);
        timeProvider.Advance(TestCoverLoadQueue.ShowDelay);

        Assert.Single(coverLoads.Started);
    }

    [Fact]
    public void ShowCover_PrefetchWhileOtherCoverLoads_WaitsForIt()
    {
        CreateOtherTrackVm().ShowCover(CoverSizes.Tile, false);
        trackVm.ShowCover(CoverSizes.Tile, true);
        timeProvider.Advance(TestCoverLoadQueue.ShowDelay);

        Assert.Equal(["Other.mp3"], coverLoads.StartedPaths);
    }

    [Fact]
    public void ShowCover_PrefetchedCoverShownAgain_LoadsWithoutWaitingForOthers()
    {
        CreateOtherTrackVm().ShowCover(CoverSizes.Tile, false);
        trackVm.ShowCover(CoverSizes.Tile, true);
        trackVm.ShowCover(CoverSizes.Tile, false);
        timeProvider.Advance(TestCoverLoadQueue.ShowDelay);

        Assert.Equal(["Other.mp3", "Psycho.mp3"], coverLoads.StartedPaths.Order());
    }

    private TrackControlVm CreateOtherTrackVm()
    {
        return TestTrackControlVm.Create("Other", coverLoads.CoverCacheMock.Object, coverLoadQueue);
    }
}
