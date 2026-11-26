using Microsoft.Extensions.Time.Testing;
using Moq;
using Nexora.Media;
using Nexora.Threading;

namespace Nexora.Tests.Media;

public class CoverLoadQueueTests
{
    private static readonly TimeSpan ShowDelay = TimeSpan.FromMilliseconds(150);
    private readonly FakeTimeProvider timeProvider = new FakeTimeProvider();
    private readonly CoverLoads coverLoads = new CoverLoads();
    private readonly List<string> loaded = [];
    private int postCount;

    [Fact]
    public void Request_BeforeShowDelay_DoesNotLoad()
    {
        var queue = CreateQueue(4);

        Request(queue, "a");
        timeProvider.Advance(ShowDelay - TimeSpan.FromMilliseconds(1));

        Assert.Empty(coverLoads.Started);
    }

    [Fact]
    public void Request_AfterShowDelay_LoadsAtSizeAndReportsLoaded()
    {
        var queue = CreateQueue(4);

        Request(queue, "a", CoverSizes.Tile);
        timeProvider.Advance(ShowDelay);
        Finish("a");

        Assert.Equal(["a"], coverLoads.StartedPaths);
        Assert.Equal(CoverSizes.Tile, coverLoads.Of("a").Size);
        Assert.Equal(["a"], loaded);
    }

    [Fact]
    public void Cancel_BeforeShowDelay_NeverLoads()
    {
        var queue = CreateQueue(4);

        Request(queue, "a").Cancel();
        timeProvider.Advance(ShowDelay);

        Assert.Empty(coverLoads.Started);
    }

    [Fact]
    public void Cancel_WhileLoading_CancelsLoadAndDoesNotReportLoaded()
    {
        var queue = CreateQueue(4);
        var request = Request(queue, "a");
        timeProvider.Advance(ShowDelay);

        request.Cancel();
        Finish("a");

        Assert.True(coverLoads.Of("a").Token.IsCancellationRequested);
        Assert.Empty(loaded);
    }

    [Fact]
    public void Request_MoreThanMaxConcurrent_LoadsMostRecentlyShownFirst()
    {
        var queue = CreateQueue(1);
        Request(queue, "a");
        timeProvider.Advance(ShowDelay);
        Request(queue, "b");
        timeProvider.Advance(TimeSpan.FromMilliseconds(10));
        Request(queue, "c");
        timeProvider.Advance(ShowDelay);
        Assert.Equal(["a"], coverLoads.StartedPaths);

        Finish("a");
        Finish("c");

        Assert.Equal(["a", "c", "b"], coverLoads.StartedPaths);
    }

    [Fact]
    public void Request_CancelledWhileWaitingForSlot_IsSkipped()
    {
        var queue = CreateQueue(1);
        Request(queue, "a");
        timeProvider.Advance(ShowDelay);
        Request(queue, "b");
        var cancelled = Request(queue, "c");
        timeProvider.Advance(ShowDelay);

        cancelled.Cancel();
        Finish("a");

        Assert.Equal(["a", "b"], coverLoads.StartedPaths);
    }

    [Fact]
    public void Request_LoadCancelled_FreesSlotForNextRequest()
    {
        var queue = CreateQueue(1);
        var request = Request(queue, "a");
        timeProvider.Advance(ShowDelay);
        Request(queue, "b");
        timeProvider.Advance(ShowDelay);

        request.Cancel();
        Finish("a", () => coverLoads.Of("a").Completion.SetCanceled(coverLoads.Of("a").Token));

        Assert.Equal(["a", "b"], coverLoads.StartedPaths);
        Assert.Empty(loaded);
    }

    [Fact]
    public void Cancel_AfterShowDelayBeforeUiThreadRuns_NeverLoads()
    {
        var postedActions = new List<Action>();
        var uiDispatcherMock = new Mock<IUiDispatcher>();
        uiDispatcherMock.Setup(it => it.Post(It.IsAny<Action>())).Callback<Action>(postedActions.Add);
        var queue = CreateQueue(4, uiDispatcherMock.Object);
        var request = Request(queue, "a");
        timeProvider.Advance(ShowDelay);

        request.Cancel();
        postedActions.ForEach(action => action());

        Assert.Single(postedActions);
        Assert.Empty(coverLoads.Started);
    }

    [Fact]
    public void Request_PrefetchShownAfterVisible_LoadsVisibleFirst()
    {
        var queue = CreateQueue(1);
        Request(queue, "a");
        timeProvider.Advance(ShowDelay);
        Request(queue, "b");
        Request(queue, "prefetch", isPrefetch: true);
        timeProvider.Advance(ShowDelay);

        Finish("a");
        Finish("b");

        Assert.Equal(["a", "b", "prefetch"], coverLoads.StartedPaths);
    }

    [Fact]
    public void Request_PrefetchWithFreeSlot_WaitsUntilVisibleLoadsEnd()
    {
        var queue = CreateQueue(4);
        Request(queue, "a");
        Request(queue, "prefetch", isPrefetch: true);
        timeProvider.Advance(ShowDelay);
        Assert.Equal(["a"], coverLoads.StartedPaths);

        Finish("a");

        Assert.Equal(["a", "prefetch"], coverLoads.StartedPaths);
    }

    [Fact]
    public void Request_OnlyPrefetches_LoadsThemConcurrently()
    {
        var queue = CreateQueue(4);
        Request(queue, "a", isPrefetch: true);
        Request(queue, "b", isPrefetch: true);
        timeProvider.Advance(ShowDelay);

        Assert.Equal(["a", "b"], coverLoads.StartedPaths.Order());
    }

    [Fact]
    public void Request_PrefetchTurnedVisible_LoadsWithoutWaiting()
    {
        var queue = CreateQueue(4);
        Request(queue, "a");
        queue.SetPrefetch(Request(queue, "b", isPrefetch: true), false);
        timeProvider.Advance(ShowDelay);

        Assert.Equal(["a", "b"], coverLoads.StartedPaths.Order());
    }

    [Fact]
    public void SetPrefetch_WaitingPrefetchTurnedVisible_LoadsBeforeEarlierVisibleOnes()
    {
        var queue = CreateQueue(1);
        Request(queue, "a");
        Request(queue, "b");
        var prefetch = Request(queue, "prefetch", isPrefetch: true);
        timeProvider.Advance(ShowDelay);

        queue.SetPrefetch(prefetch, false);
        Finish("a");

        Assert.Equal(["a", "prefetch"], coverLoads.StartedPaths);
    }

    [Fact]
    public void SetPrefetch_LastVisibleLoadTurnedPrefetch_StartsWaitingPrefetch()
    {
        var queue = CreateQueue(4);
        var visible = Request(queue, "a");
        Request(queue, "prefetch", isPrefetch: true);
        timeProvider.Advance(ShowDelay);

        queue.SetPrefetch(visible, true);

        Assert.Equal(["a", "prefetch"], coverLoads.StartedPaths);
    }

    [Fact]
    public void SetPrefetch_Unchanged_KeepsOrder()
    {
        var queue = CreateQueue(1);
        Request(queue, "a");
        var first = Request(queue, "b");
        Request(queue, "c");
        timeProvider.Advance(ShowDelay);

        queue.SetPrefetch(first, false);
        Finish("a");
        Finish("c");

        Assert.Equal(["a", "c", "b"], coverLoads.StartedPaths);
    }

    [Fact]
    public void Request_PrefetchShownBeforeVisible_WaitsForVisibleStillInDelay()
    {
        var queue = CreateQueue(4);
        Request(queue, "prefetch", isPrefetch: true);
        timeProvider.Advance(TimeSpan.FromMilliseconds(10));
        Request(queue, "a");
        timeProvider.Advance(ShowDelay - TimeSpan.FromMilliseconds(10));
        Assert.Empty(coverLoads.Started);

        timeProvider.Advance(TimeSpan.FromMilliseconds(10));

        Assert.Equal(["a"], coverLoads.StartedPaths);
    }

    [Fact]
    public void Cancel_LastVisibleInDelay_StartsWaitingPrefetch()
    {
        var queue = CreateQueue(4);
        Request(queue, "prefetch", isPrefetch: true);
        timeProvider.Advance(TimeSpan.FromMilliseconds(10));
        var visible = Request(queue, "a");
        timeProvider.Advance(ShowDelay - TimeSpan.FromMilliseconds(10));

        queue.Cancel(visible);

        Assert.Equal(["prefetch"], coverLoads.StartedPaths);
    }

    private CoverLoadQueue CreateQueue(int maxConcurrentLoads)
    {
        var uiDispatcherMock = new Mock<IUiDispatcher>();
        uiDispatcherMock
            .Setup(it => it.Post(It.IsAny<Action>()))
            .Callback<Action>(it =>
            {
                it();
                Interlocked.Increment(ref postCount);
            });

        return CreateQueue(maxConcurrentLoads, uiDispatcherMock.Object);
    }

    private CoverLoadQueue CreateQueue(int maxConcurrentLoads, IUiDispatcher uiDispatcher)
    {
        return new CoverLoadQueue(
            coverLoads.CoverCacheMock.Object,
            timeProvider,
            uiDispatcher,
            ShowDelay,
            maxConcurrentLoads
        );
    }

    private void Finish(string audioPath)
    {
        Finish(audioPath, () => coverLoads.Of(audioPath).Completion.SetResult(null));
    }

    private void Finish(string audioPath, Action complete)
    {
        int postsBefore = Volatile.Read(ref postCount);
        complete();
        Assert.True(SpinWait.SpinUntil(() => Volatile.Read(ref postCount) > postsBefore, TimeSpan.FromSeconds(5)));
    }

    private CoverRequest Request(
        CoverLoadQueue queue,
        string audioPath,
        double size = CoverSizes.Thumbnail,
        bool isPrefetch = false)
    {
        return queue.Request(audioPath, size, isPrefetch, _ => loaded.Add(audioPath));
    }
}
