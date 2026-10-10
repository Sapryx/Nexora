using System;

namespace Nexora.Threading;

public interface IUiDispatcher
{
    void Post(Action action);
}
