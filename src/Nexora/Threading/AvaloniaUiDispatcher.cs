using Avalonia.Threading;

namespace Nexora.Threading;

public class AvaloniaUiDispatcher : IUiDispatcher
{
    public void Post(Action action)
    {
        Dispatcher.UIThread.Post(action);
    }

    public void Post(Action action, DispatcherPriority priority)
    {
        Dispatcher.UIThread.Post(action, priority);
    }

    public void AddUnhandledExceptionHandler(Action<Exception> handler)
    {
        Dispatcher.UIThread.UnhandledException += (_, e) => handler(e.Exception);
    }
}
