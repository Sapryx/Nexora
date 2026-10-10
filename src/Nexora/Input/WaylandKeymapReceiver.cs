using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace Nexora.Input;

public unsafe partial class WaylandKeymapReceiver
{
    private const string WaylandLibrary = "libwayland-client.so.0";
    private const uint DisplayGetRegistryOpcode = 1;
    private const uint RegistryBindOpcode = 0;
    private const uint SeatGetKeyboardOpcode = 1;
    private const uint SeatVersion = 1;
    private const uint KeyboardCapability = 2;
    private const uint KeymapFormatXkbV1 = 1;
    private const int MaxRoundtrips = 3;
    private const string SeatInterfaceName = "wl_seat";

    private static readonly nint* RegistryListener = CreateListener(
        (nint)(delegate* unmanaged[Cdecl]<nint, nint, uint, byte*, uint, void>)&OnRegistryGlobal,
        (nint)(delegate* unmanaged[Cdecl]<nint, nint, uint, void>)&OnRegistryGlobalRemove);

    private static readonly nint* SeatListener = CreateListener(
        (nint)(delegate* unmanaged[Cdecl]<nint, nint, uint, void>)&OnSeatCapabilities,
        (nint)(delegate* unmanaged[Cdecl]<nint, nint, byte*, void>)&OnSeatName);

    private static readonly nint* KeyboardListener = CreateListener(
        (nint)(delegate* unmanaged[Cdecl]<nint, nint, uint, int, uint, void>)&OnKeyboardKeymap,
        (nint)(delegate* unmanaged[Cdecl]<nint, nint, uint, nint, nint, void>)&OnKeyboardEnter,
        (nint)(delegate* unmanaged[Cdecl]<nint, nint, uint, nint, void>)&OnKeyboardLeave,
        (nint)(delegate* unmanaged[Cdecl]<nint, nint, uint, uint, uint, uint, void>)&OnKeyboardKey,
        (nint)(delegate* unmanaged[Cdecl]<nint, nint, uint, uint, uint, uint, uint, void>)&OnKeyboardModifiers,
        (nint)(delegate* unmanaged[Cdecl]<nint, nint, int, int, void>)&OnKeyboardRepeatInfo);

    private nint userData;
    private nint seatInterface;
    private nint keyboardInterface;
    private nint registry;
    private nint seat;
    private nint keyboard;
    public bool SeatFound { get; private set; }
    public bool KeyboardFound { get; private set; }
    public string? Keymap { get; private set; }
    public string? KeymapError { get; private set; }

    public void Receive()
    {
        nint display = DisplayConnect(null);

        if(display == 0)
        {
            throw new InvalidOperationException("Failed to connect to the Wayland display");
        }

        var handle = GCHandle.Alloc(this);

        try
        {
            userData = GCHandle.ToIntPtr(handle);

            nint waylandLibrary = NativeLibrary.Load(WaylandLibrary);
            nint registryInterface = NativeLibrary.GetExport(waylandLibrary, "wl_registry_interface");
            seatInterface = NativeLibrary.GetExport(waylandLibrary, "wl_seat_interface");
            keyboardInterface = NativeLibrary.GetExport(waylandLibrary, "wl_keyboard_interface");

            nint* arguments = stackalloc nint[1];
            arguments[0] = 0;
            registry = ProxyMarshalArrayFlags(display, DisplayGetRegistryOpcode, registryInterface, ProxyGetVersion(display), 0, arguments);
            ProxyAddListener(registry, RegistryListener, userData);

            for(int i = 0; i < MaxRoundtrips && Keymap == null && KeymapError == null; i++)
            {
                if(DisplayRoundtrip(display) < 0)
                {
                    throw new InvalidOperationException("Wayland display roundtrip failed");
                }
            }
        }
        finally
        {
            DestroyProxy(keyboard);
            DestroyProxy(seat);
            DestroyProxy(registry);
            DisplayDisconnect(display);
            handle.Free();
        }
    }

    private void BindSeat(uint name, byte* interfaceName)
    {
        nint* arguments = stackalloc nint[4];
        arguments[0] = (nint)name;
        arguments[1] = (nint)interfaceName;
        arguments[2] = (nint)SeatVersion;
        arguments[3] = 0;

        seat = ProxyMarshalArrayFlags(registry, RegistryBindOpcode, seatInterface, SeatVersion, 0, arguments);
        ProxyAddListener(seat, SeatListener, userData);
        SeatFound = true;
    }

    private void GetKeyboard()
    {
        nint* arguments = stackalloc nint[1];
        arguments[0] = 0;

        keyboard = ProxyMarshalArrayFlags(seat, SeatGetKeyboardOpcode, keyboardInterface, ProxyGetVersion(seat), 0, arguments);
        ProxyAddListener(keyboard, KeyboardListener, userData);
        KeyboardFound = true;
    }

    private void ReadKeymap(uint format, int fileDescriptor, uint size)
    {
        using var file = new SafeFileHandle(fileDescriptor, true);

        if(format != KeymapFormatXkbV1)
        {
            KeymapError = $"Unsupported keymap format {format}";
            return;
        }

        try
        {
            var buffer = new byte[size];
            int length = RandomAccess.Read(file, buffer, 0);
            Keymap = Encoding.UTF8.GetString(buffer, 0, length).TrimEnd('\0');
        }
        catch(Exception ex)
        {
            KeymapError = ex.Message;
        }
    }

    private static void DestroyProxy(nint proxy)
    {
        if(proxy != 0)
        {
            ProxyDestroy(proxy);
        }
    }

    private static WaylandKeymapReceiver FromUserData(nint userData)
    {
        return (WaylandKeymapReceiver)GCHandle.FromIntPtr(userData).Target!;
    }

    private static nint* CreateListener(params nint[] callbacks)
    {
        var listener = (nint*)NativeMemory.Alloc((nuint)(callbacks.Length * sizeof(nint)));

        for(int i = 0; i < callbacks.Length; i++)
        {
            listener[i] = callbacks[i];
        }

        return listener;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void OnRegistryGlobal(nint userData, nint registry, uint name, byte* interfaceName, uint version)
    {
        var receiver = FromUserData(userData);

        if(!receiver.SeatFound && Marshal.PtrToStringUTF8((nint)interfaceName) == SeatInterfaceName)
        {
            receiver.BindSeat(name, interfaceName);
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void OnRegistryGlobalRemove(nint userData, nint registry, uint name)
    {
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void OnSeatCapabilities(nint userData, nint seat, uint capabilities)
    {
        var receiver = FromUserData(userData);

        if(!receiver.KeyboardFound && (capabilities & KeyboardCapability) != 0)
        {
            receiver.GetKeyboard();
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void OnSeatName(nint userData, nint seat, byte* name)
    {
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void OnKeyboardKeymap(nint userData, nint keyboard, uint format, int fileDescriptor, uint size)
    {
        FromUserData(userData).ReadKeymap(format, fileDescriptor, size);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void OnKeyboardEnter(nint userData, nint keyboard, uint serial, nint surface, nint keys)
    {
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void OnKeyboardLeave(nint userData, nint keyboard, uint serial, nint surface)
    {
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void OnKeyboardKey(nint userData, nint keyboard, uint serial, uint time, uint key, uint state)
    {
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void OnKeyboardModifiers(nint userData, nint keyboard, uint serial, uint depressed, uint latched, uint locked, uint group)
    {
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void OnKeyboardRepeatInfo(nint userData, nint keyboard, int rate, int delay)
    {
    }

    [LibraryImport(WaylandLibrary, EntryPoint = "wl_display_connect")]
    private static partial nint DisplayConnect(byte* name);

    [LibraryImport(WaylandLibrary, EntryPoint = "wl_display_disconnect")]
    private static partial void DisplayDisconnect(nint display);

    [LibraryImport(WaylandLibrary, EntryPoint = "wl_display_roundtrip")]
    private static partial int DisplayRoundtrip(nint display);

    [LibraryImport(WaylandLibrary, EntryPoint = "wl_proxy_marshal_array_flags")]
    private static partial nint ProxyMarshalArrayFlags(nint proxy, uint opcode, nint @interface, uint version, uint flags, nint* arguments);

    [LibraryImport(WaylandLibrary, EntryPoint = "wl_proxy_add_listener")]
    private static partial int ProxyAddListener(nint proxy, nint* listener, nint userData);

    [LibraryImport(WaylandLibrary, EntryPoint = "wl_proxy_get_version")]
    private static partial uint ProxyGetVersion(nint proxy);

    [LibraryImport(WaylandLibrary, EntryPoint = "wl_proxy_destroy")]
    private static partial void ProxyDestroy(nint proxy);
}
