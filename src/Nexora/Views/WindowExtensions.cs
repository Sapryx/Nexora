using Avalonia.Controls;

namespace Nexora.Views;

public static class WindowExtensions
{
    public static void ToggleMaximize(this Window window)
    {
        window.WindowState = window.WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }
}
