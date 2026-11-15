using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Nexora.Views;

namespace Nexora.UiTests.Views;

public class FullScreenTests
{
    [AvaloniaFact]
    public void F11_PressedTwice_EntersAndLeavesFullScreen()
    {
        using var app = new TestMainWindow();

        PressF11(app);
        Assert.Equal(WindowState.FullScreen, app.Window.WindowState);

        PressF11(app);
        Assert.Equal(WindowState.Normal, app.Window.WindowState);
    }

    [AvaloniaFact]
    public void F11_LeavingFullScreen_RestoresMaximizedState()
    {
        using var app = new TestMainWindow();
        app.Window.WindowState = WindowState.Maximized;
        Dispatcher.UIThread.RunJobs();

        PressF11(app);
        PressF11(app);

        Assert.Equal(WindowState.Maximized, app.Window.WindowState);
    }

    [AvaloniaFact]
    public void F11_InFocusedSearchBox_EntersFullScreen()
    {
        using var app = new TestMainWindow();
        var textBox = app.Window.GetVisualDescendants().OfType<SearchBar>().Single()
            .GetVisualDescendants().OfType<TextBox>().First();
        textBox.Focus();
        Dispatcher.UIThread.RunJobs();

        PressF11(app);

        Assert.Equal(WindowState.FullScreen, app.Window.WindowState);
    }

    private static void PressF11(TestMainWindow app)
    {
        app.Window.KeyPress(Key.F11, RawInputModifiers.None, PhysicalKey.F11, null);
        app.Window.KeyRelease(Key.F11, RawInputModifiers.None, PhysicalKey.F11, null);
        Dispatcher.UIThread.RunJobs();
    }
}
