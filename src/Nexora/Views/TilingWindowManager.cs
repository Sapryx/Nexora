namespace Nexora.Views;

public static class TilingWindowManager
{
    private static readonly HashSet<string> SessionNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "awesome",
        "bspwm",
        "dwl",
        "dwm",
        "herbstluftwm",
        "hyprland",
        "hyprland-uwsm",
        "i3",
        "i3-with-shmlog",
        "leftwm",
        "mango",
        "miracle-wm",
        "niri",
        "qtile",
        "qtile-wayland",
        "river",
        "spectrwm",
        "stumpwm",
        "sway",
        "swayfx",
        "xmonad"
    };

    private static readonly string[] SessionVariables = [
        "XDG_CURRENT_DESKTOP",
        "XDG_SESSION_DESKTOP",
        "DESKTOP_SESSION"
    ];

    private static readonly string[] InstanceVariables = [
        "HYPRLAND_INSTANCE_SIGNATURE",
        "I3SOCK",
        "NIRI_SOCKET",
        "SWAYSOCK"
    ];

    public static bool IsPresent(Func<string, string?> getVariable)
    {
        return SessionVariables.SelectMany(name =>
                getVariable(name)?.Split(':', StringSplitOptions.TrimEntries) ?? [])
            .Any(SessionNames.Contains)
            || InstanceVariables.Any(name => !string.IsNullOrWhiteSpace(getVariable(name)));
    }
}
