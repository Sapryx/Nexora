using Microsoft.Extensions.Time.Testing;
using Moq;
using Nexora.Threading;

namespace Nexora.Tests.Threading;

public class ThrottlerTests
{
    private static readonly TimeSpan Interval = TimeSpan.FromMilliseconds(150);
    private readonly FakeTimeProvider timeProvider = new FakeTimeProvider();
    private readonly List<Action> postedActions = [];
    private readonly List<int> runs = [];
    private readonly Throttler throttler;

    public ThrottlerTests()
    {
        var uiDispatcherMock = new Mock<IUiDispatcher>();
        uiDispatcherMock
            .Setup(it => it.Post(It.IsAny<Action>()))
            .Callback<Action>(postedActions.Add);

        throttler = new Throttler(timeProvider, uiDispatcherMock.Object, Interval);
    }

    [Fact]
    public void Run_Idle_RunsActionImmediately()
    {
        throttler.Run(() => runs.Add(1));

        Assert.Equal([1], runs);
    }

    [Fact]
    public void Run_WithinInterval_RunsOnlyLatestActionWhenIntervalElapses()
    {
        throttler.Run(() => runs.Add(1));
        throttler.Run(() => runs.Add(2));
        throttler.Run(() => runs.Add(3));
        Assert.Equal([1], runs);

        Advance(Interval);

        Assert.Equal([1, 3], runs);
    }

    [Fact]
    public void Run_PendingActionRan_StartsNewInterval()
    {
        throttler.Run(() => runs.Add(1));
        throttler.Run(() => runs.Add(2));
        Advance(Interval);

        throttler.Run(() => runs.Add(3));
        Advance(Interval - TimeSpan.FromMilliseconds(1));
        Assert.Equal([1, 2], runs);

        Advance(TimeSpan.FromMilliseconds(1));
        Assert.Equal([1, 2, 3], runs);
    }

    [Fact]
    public void Run_IntervalElapsedWithoutPendingAction_RunsNextActionImmediately()
    {
        throttler.Run(() => runs.Add(1));
        Advance(Interval);

        throttler.Run(() => runs.Add(2));

        Assert.Equal([1, 2], runs);
    }

    [Fact]
    public void Flush_PendingAction_RunsItImmediatelyAndNotAgainLater()
    {
        throttler.Run(() => runs.Add(1));
        throttler.Run(() => runs.Add(2));

        throttler.Flush();
        Assert.Equal([1, 2], runs);

        Advance(Interval);
        Assert.Equal([1, 2], runs);
    }

    [Fact]
    public void Flush_NoPendingAction_RunsNothingAndNextActionRunsImmediately()
    {
        throttler.Run(() => runs.Add(1));

        throttler.Flush();
        throttler.Run(() => runs.Add(2));

        Assert.Equal([1, 2], runs);
    }

    [Fact]
    public void Flush_IntervalElapsedButNotYetHandled_IgnoresStaleElapse()
    {
        throttler.Run(() => runs.Add(1));
        timeProvider.Advance(Interval);
        throttler.Flush();
        throttler.Run(() => runs.Add(2));
        throttler.Run(() => runs.Add(3));

        RunPostedActions();

        Assert.Equal([1, 2], runs);
    }

    private void Advance(TimeSpan time)
    {
        timeProvider.Advance(time);
        RunPostedActions();
    }

    private void RunPostedActions()
    {
        var actions = postedActions.ToList();
        postedActions.Clear();
        actions.ForEach(action => action());
    }
}
