using System;
using Avalonia.Threading;

namespace Nexora.Threading;

public class AvaloniaUiDispatcher : IUiDispatcher
{
    public void Post(Action action)
    {
        Dispatcher.UIThread.Post(action);
    }
}
