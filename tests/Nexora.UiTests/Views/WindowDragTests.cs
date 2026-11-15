using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;

namespace Nexora.UiTests.Views;

public class WindowDragTests
{
    [AvaloniaFact]
    public void TopStrip_PressOnEmptySpot_StartsMoveDrag()
    {
        using var app = new TestMainWindow();

        Press(app, new Point(900, 10));

        Assert.Equal(1, app.Window.MoveDragCount);
    }

    [AvaloniaFact]
    public void BelowTopStrip_PressOnEmptySpot_DoesNotStartMoveDrag()
    {
        using var app = new TestMainWindow();

        Press(app, new Point(900, 100));

        Assert.Equal(0, app.Window.MoveDragCount);
    }

    [AvaloniaFact]
    public void TopStrip_DoubleClick_TogglesMaximizedAndRestored()
    {
        using var app = new TestMainWindow();
        var point = new Point(900, 10);

        app.Window.MouseDown(point, MouseButton.Left);
        app.Window.MouseUp(point, MouseButton.Left);
        app.Window.MouseDown(point, MouseButton.Left);
        app.Window.MouseUp(point, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(WindowState.Maximized, app.Window.WindowState);

        point = new Point(1000, 10);
        app.Window.MouseDown(point, MouseButton.Left);
        app.Window.MouseUp(point, MouseButton.Left);
        app.Window.MouseDown(point, MouseButton.Left);
        app.Window.MouseUp(point, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(WindowState.Normal, app.Window.WindowState);
    }

    private static void Press(TestMainWindow app, Point point)
    {
        app.Window.MouseDown(point, MouseButton.Left);
        app.Window.MouseUp(point, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
    }
}
