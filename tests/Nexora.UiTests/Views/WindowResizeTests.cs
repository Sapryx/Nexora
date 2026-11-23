using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;

namespace Nexora.UiTests.Views;

public class WindowResizeTests
{
    [AvaloniaFact]
    public void Edges_Press_StartsResizeDragOnThatEdge()
    {
        using var app = new TestMainWindow();

        app.Click(new Point(1278, 400));
        app.Click(new Point(640, 798));
        app.Click(new Point(1278, 798));
        app.Click(new Point(640, 2));

        WindowEdge[] expected = [
            WindowEdge.East,
            WindowEdge.South,
            WindowEdge.SouthEast,
            WindowEdge.North
        ];

        Assert.Equal(expected, app.Window.ResizeDragEdges);
        Assert.Equal(0, app.Window.MoveDragCount);
    }

    [AvaloniaFact]
    public void WindowButtonsCorner_Press_StartsResizeDragInsteadOfClosing()
    {
        using var app = new TestMainWindow();

        app.Click(new Point(1278, 2));

        Assert.Equal([WindowEdge.NorthEast], app.Window.ResizeDragEdges);
        Assert.True(app.Window.IsVisible);
    }

    [AvaloniaFact]
    public void Interior_Press_DoesNotStartResizeDrag()
    {
        using var app = new TestMainWindow();

        app.Click(new Point(900, 10));
        app.Click(new Point(1270, 400));

        Assert.Empty(app.Window.ResizeDragEdges);
    }

    [AvaloniaFact]
    public void MaximizedWindow_PressOnEdge_DoesNotStartResizeDrag()
    {
        using var app = new TestMainWindow();
        app.Window.WindowState = WindowState.Maximized;
        Dispatcher.UIThread.RunJobs();

        app.Click(new Point(app.Window.Bounds.Width - 2, 400));

        Assert.Empty(app.Window.ResizeDragEdges);
    }

    [AvaloniaFact]
    public void Edge_Hover_ShowsResizeCursorAndResetsInside()
    {
        using var app = new TestMainWindow();

        app.Window.MouseMove(new Point(1278, 400));
        Dispatcher.UIThread.RunJobs();

        Assert.NotNull(app.Window.Cursor);

        app.Window.MouseMove(new Point(900, 400));
        Dispatcher.UIThread.RunJobs();

        Assert.Null(app.Window.Cursor);
    }
}
