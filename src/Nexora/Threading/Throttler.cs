namespace Nexora.Threading;

public class Throttler
{
    private readonly TimeProvider timeProvider;
    private readonly IUiDispatcher uiDispatcher;
    private readonly TimeSpan interval;
    private ITimer? intervalTimer;
    private object? intervalToken;
    private Action? pendingAction;

    public Throttler(TimeProvider timeProvider, IUiDispatcher uiDispatcher, TimeSpan interval)
    {
        this.timeProvider = timeProvider;
        this.uiDispatcher = uiDispatcher;
        this.interval = interval;
    }

    public void Run(Action action)
    {
        if(intervalToken != null)
        {
            pendingAction = action;
            return;
        }

        action();
        StartInterval();
    }

    public void Flush()
    {
        StopInterval();
        RunPendingAction();
    }

    private void StartInterval()
    {
        object token = new object();
        intervalToken = token;
        intervalTimer = timeProvider.CreateTimer(
            _ => uiDispatcher.Post(() => OnIntervalElapsed(token)),
            null,
            interval,
            Timeout.InfiniteTimeSpan
        );
    }

    private void StopInterval()
    {
        intervalTimer?.Dispose();
        intervalTimer = null;
        intervalToken = null;
    }

    private void OnIntervalElapsed(object token)
    {
        if(token != intervalToken)
        {
            return;
        }

        StopInterval();

        if(pendingAction != null)
        {
            RunPendingAction();
            StartInterval();
        }
    }

    private void RunPendingAction()
    {
        var action = pendingAction;
        pendingAction = null;
        action?.Invoke();
    }
}
