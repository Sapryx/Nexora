using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Core.Logging;
using Core.Search;
using Microsoft.Extensions.Logging;

namespace Nexora.Input;

public unsafe partial class WaylandKeyboardLayoutProvider : IKeyboardLayoutProvider
{
    private const string XkbLibrary = "libxkbcommon.so.0";
    private const int XkbKeymapFormatTextV1 = 1;
    private const int XkbKeycodeOffset = 8;
    private const uint BaseLevel = 0;
    private const uint ShiftLevel = 1;
    private readonly ILogger logger;

    public WaylandKeyboardLayoutProvider(ILogger<WaylandKeyboardLayoutProvider> logger)
    {
        this.logger = logger;
    }

    public IReadOnlyList<KeyboardLayout> GetLayouts()
    {
        logger.Info($"Reading keyboard layouts from the Wayland compositor keymap");

        var receiver = new WaylandKeymapReceiver();
        receiver.Receive();

        logger.Debug($"Wayland seat found: {receiver.SeatFound}, keyboard found: {receiver.KeyboardFound}");

        if(receiver.KeymapError != null)
        {
            logger.Warn($"Failed to read the Wayland keymap: {receiver.KeymapError}");
            return [];
        }

        if(receiver.Keymap == null)
        {
            logger.Warn($"Wayland compositor did not send a keymap");
            return [];
        }

        logger.Debug($"Received XKB keymap of {receiver.Keymap.Length} characters");

        return ParseKeymap(receiver.Keymap);
    }

    private IReadOnlyList<KeyboardLayout> ParseKeymap(string keymapText)
    {
        nint context = ContextNew(0);

        if(context == 0)
        {
            throw new InvalidOperationException("Failed to create an XKB context");
        }

        nint keymap = 0;

        try
        {
            keymap = KeymapNewFromString(context, keymapText, XkbKeymapFormatTextV1, 0);

            if(keymap == 0)
            {
                throw new InvalidOperationException("Failed to compile the XKB keymap");
            }

            uint layoutCount = KeymapNumLayouts(keymap);
            logger.Debug($"XKB keymap contains {layoutCount} layouts");

            return Enumerable.Range(0, (int)layoutCount).Select(it => CreateLayout(keymap, (uint)it)).ToList();
        }
        finally
        {
            if(keymap != 0)
            {
                KeymapUnref(keymap);
            }

            ContextUnref(context);
        }
    }

    private static KeyboardLayout CreateLayout(nint keymap, uint layout)
    {
        Dictionary<KeyboardKey, char> characters = [];

        foreach(int scanCode in KeyboardScanCodes.MainBlock)
        {
            uint keycode = (uint)(scanCode + XkbKeycodeOffset);
            uint keyLayoutCount = KeymapNumLayoutsForKey(keymap, keycode);

            if(keyLayoutCount != 0)
            {
                uint keyLayout = layout % keyLayoutCount;
                AddCharacter(characters, keymap, keycode, keyLayout, scanCode, false);
                AddCharacter(characters, keymap, keycode, keyLayout, scanCode, true);
            }
        }

        string name = Marshal.PtrToStringUTF8(KeymapLayoutGetName(keymap, layout)) ?? $"Layout {layout}";

        return new KeyboardLayout(name, characters);
    }

    private static void AddCharacter(Dictionary<KeyboardKey, char> characters, nint keymap, uint keycode, uint layout, int scanCode, bool shifted)
    {
        uint* symbols;
        int count = KeymapKeyGetSymsByLevel(keymap, keycode, layout, shifted ? ShiftLevel : BaseLevel, &symbols);

        if(count != 1)
        {
            return;
        }

        uint codePoint = KeysymToUtf32(symbols[0]);

        if(codePoint != 0 && codePoint <= char.MaxValue && !char.IsControl((char)codePoint))
        {
            characters[new KeyboardKey(scanCode, shifted)] = (char)codePoint;
        }
    }

    [LibraryImport(XkbLibrary, EntryPoint = "xkb_context_new")]
    private static partial nint ContextNew(int flags);

    [LibraryImport(XkbLibrary, EntryPoint = "xkb_context_unref")]
    private static partial void ContextUnref(nint context);

    [LibraryImport(XkbLibrary, EntryPoint = "xkb_keymap_new_from_string", StringMarshalling = StringMarshalling.Utf8)]
    private static partial nint KeymapNewFromString(nint context, string keymap, int format, int flags);

    [LibraryImport(XkbLibrary, EntryPoint = "xkb_keymap_unref")]
    private static partial void KeymapUnref(nint keymap);

    [LibraryImport(XkbLibrary, EntryPoint = "xkb_keymap_num_layouts")]
    private static partial uint KeymapNumLayouts(nint keymap);

    [LibraryImport(XkbLibrary, EntryPoint = "xkb_keymap_num_layouts_for_key")]
    private static partial uint KeymapNumLayoutsForKey(nint keymap, uint keycode);

    [LibraryImport(XkbLibrary, EntryPoint = "xkb_keymap_layout_get_name")]
    private static partial nint KeymapLayoutGetName(nint keymap, uint layout);

    [LibraryImport(XkbLibrary, EntryPoint = "xkb_keymap_key_get_syms_by_level")]
    private static partial int KeymapKeyGetSymsByLevel(nint keymap, uint keycode, uint layout, uint level, uint** symbols);

    [LibraryImport(XkbLibrary, EntryPoint = "xkb_keysym_to_utf32")]
    private static partial uint KeysymToUtf32(uint keysym);
}
