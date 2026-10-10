using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using Core.Logging;
using Core.Search;
using Microsoft.Extensions.Logging;

namespace Nexora.Input;

public unsafe partial class WindowsKeyboardLayoutProvider : IKeyboardLayoutProvider
{
    private const string User32Library = "user32.dll";
    private const uint MapScanCodeToVirtualKey = 3;
    private const uint DoNotChangeKeyboardState = 4;
    private const int ShiftVirtualKey = 0x10;
    private const byte KeyPressed = 0x80;
    private const int KeyboardStateLength = 256;
    private const int CharacterBufferLength = 8;
    private readonly ILogger logger;

    public WindowsKeyboardLayoutProvider(ILogger<WindowsKeyboardLayoutProvider> logger)
    {
        this.logger = logger;
    }

    public IReadOnlyList<KeyboardLayout> GetLayouts()
    {
        logger.Info($"Reading keyboard layouts via Windows API");

        int count = GetKeyboardLayoutList(0, null);
        var layoutHandles = new nint[count];

        fixed(nint* layoutHandlesPointer = layoutHandles)
        {
            count = GetKeyboardLayoutList(count, layoutHandlesPointer);
        }

        logger.Debug($"Windows returned {count} keyboard layouts: {string.Join(", ", layoutHandles.Take(count).Select(it => $"0x{it:X8}"))}");

        return layoutHandles.Take(count).Select(CreateLayout).ToList();
    }

    private static KeyboardLayout CreateLayout(nint layoutHandle)
    {
        Dictionary<KeyboardKey, char> characters = [];

        foreach(int scanCode in KeyboardScanCodes.MainBlock)
        {
            uint virtualKey = MapVirtualKeyEx((uint)scanCode, MapScanCodeToVirtualKey, layoutHandle);

            if(virtualKey != 0)
            {
                AddCharacter(characters, layoutHandle, virtualKey, scanCode, false);
                AddCharacter(characters, layoutHandle, virtualKey, scanCode, true);
            }
        }

        return new KeyboardLayout(GetLayoutName(layoutHandle), characters);
    }

    private static void AddCharacter(Dictionary<KeyboardKey, char> characters, nint layoutHandle, uint virtualKey, int scanCode, bool shifted)
    {
        byte* keyboardState = stackalloc byte[KeyboardStateLength];
        char* buffer = stackalloc char[CharacterBufferLength];
        keyboardState[ShiftVirtualKey] = shifted ? KeyPressed : (byte)0;

        int length = ToUnicodeEx(virtualKey, (uint)scanCode, keyboardState, buffer, CharacterBufferLength, DoNotChangeKeyboardState, layoutHandle);

        if(length == 1 && !char.IsControl(buffer[0]))
        {
            characters[new KeyboardKey(scanCode, shifted)] = buffer[0];
        }
    }

    private static string GetLayoutName(nint layoutHandle)
    {
        int languageId = (int)(layoutHandle & 0xFFFF);

        try
        {
            return $"{CultureInfo.GetCultureInfo(languageId).EnglishName} (0x{layoutHandle:X8})";
        }
        catch(CultureNotFoundException)
        {
            return $"0x{layoutHandle:X8}";
        }
    }

    [LibraryImport(User32Library)]
    private static partial int GetKeyboardLayoutList(int count, nint* layoutHandles);

    [LibraryImport(User32Library, EntryPoint = "MapVirtualKeyExW")]
    private static partial uint MapVirtualKeyEx(uint code, uint mapType, nint layoutHandle);

    [LibraryImport(User32Library)]
    private static partial int ToUnicodeEx(uint virtualKey, uint scanCode, byte* keyboardState, char* buffer, int bufferLength, uint flags, nint layoutHandle);
}
