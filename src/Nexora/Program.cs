using Avalonia;

namespace Nexora;

sealed class Program
{
    private const string InstanceMutexName = "Nexora.SingleInstance";

    [STAThread]
    public static void Main(string[] args)
    {
#if !DEBUG
        var mutexOptions = new NamedWaitHandleOptions() { CurrentUserOnly = true, CurrentSessionOnly = false };
        using var instanceMutex = new Mutex(true, InstanceMutexName, mutexOptions, out bool isFirstInstance);

        if(!isFirstInstance)
        {
            return;
        }
#endif

        CompositionRoot.Run(args);
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        return CompositionRoot.BuildDesignerApp();
    }
}
