using System.Runtime.InteropServices;

namespace Nexora.Logging;

public static partial class WindowsConsole
{
    private const string Kernel32Library = "kernel32.dll";
    private const int StandardOutputHandle = -11;
    private const int AttachParentProcess = -1;
    private const nint InvalidHandle = -1;

    public static bool TryAttachStandardOutput()
    {
        nint handle = GetStdHandle(StandardOutputHandle);

        if(handle != 0 && handle != InvalidHandle)
        {
            return true;
        }

        return AttachConsole(AttachParentProcess);
    }

    [LibraryImport(Kernel32Library)]
    private static partial nint GetStdHandle(int standardHandle);

    [LibraryImport(Kernel32Library)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool AttachConsole(int processId);
}
