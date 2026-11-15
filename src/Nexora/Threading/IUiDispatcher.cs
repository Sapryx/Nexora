using Avalonia.Threading;

namespace Nexora.Threading;

public interface IUiDispatcher
{
    void Post(Action action);
    void Post(Action action, DispatcherPriority priority);
    void AddUnhandledExceptionHandler(Action<Exception> handler);
}
