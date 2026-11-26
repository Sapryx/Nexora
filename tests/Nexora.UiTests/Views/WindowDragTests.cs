using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Nexora.Controls;
using Nexora.Views;

namespace Nexora.UiTests.Views;

public class WindowDragTests
{
    [AvaloniaFact]
    public void TilingSession_HidesCaptionButtonsAndDisablesTopDragWithoutMovingLibraryControls()
    {
        using var stacking = new TestMainWindow(testName: "StackingWindowControls");
        using var tiling = new TestMainWindow(showWindowControls: false);

        var point = new Point(900, 10);
        tiling.Click(point);
        tiling.Click(point);

        Assert.False(tiling.Find<WindowButtons>().IsEffectivelyVisible);
        Assert.Equal(0, tiling.Window.MoveDragCount);
        Assert.Equal(WindowState.Normal, tiling.Window.WindowState);
        Assert.Equal(stacking.Find<SearchBar>().Bounds, tiling.Find<SearchBar>().Bounds);
        Assert.Equal(
            stacking.Find<SegmentedControl>("LibraryViewSwitch").Bounds,
            tiling.Find<SegmentedControl>("LibraryViewSwitch").Bounds
        );
    }

    [AvaloniaFact]
    public void TopStrip_PressOnEmptySpot_StartsMoveDrag()
    {
        using var app = new TestMainWindow();

        app.Click(new Point(900, 10));

        Assert.Equal(1, app.Window.MoveDragCount);
    }

    [AvaloniaFact]
    public void MaximizedTopStrip_PressOnEmptySpot_StartsMoveDrag()
    {
        using var app = new TestMainWindow();
        app.Window.WindowState = WindowState.Maximized;
        Dispatcher.UIThread.RunJobs();

        app.Click(new Point(900, 10));
        app.Window.WindowState = WindowState.Normal;
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(1, app.Window.MoveDragCount);
        Assert.Equal(WindowState.Normal, app.Window.WindowState);
    }

    [AvaloniaFact]
    public void NormalTopStrip_PressOnEmptySpot_StartsSnappableMoveDrag()
    {
        using var app = new TestMainWindow();

        app.Click(new Point(900, 10));

        Assert.Equal(WindowState.Normal, app.Window.WindowState);
        Assert.True(app.Window.CanResize);
        Assert.Equal(WindowDecorations.BorderOnly, app.Window.WindowDecorations);
        Assert.Equal(1, app.Window.MoveDragCount);
    }

    [AvaloniaFact]
    public void BelowTopStrip_PressOnEmptySpot_DoesNotStartMoveDrag()
    {
        using var app = new TestMainWindow();

        app.Click(new Point(900, 100));

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

    [AvaloniaTheory]
    [InlineData(typeof(SearchBar))]
    [InlineData(typeof(WindowButtons))]
    [InlineData(typeof(SegmentedControl))]
    public void TopStrip_PressOnInteractiveControl_DoesNotStartMoveDrag(Type controlType)
    {
        using var app = new TestMainWindow();

        var controls = app.Window.GetVisualDescendants()
            .Where(c => c.GetType() == controlType)
            .OfType<Control>()
            .Where(c => c.IsEffectivelyVisible)
            .ToList();

        foreach(var control in controls)
        {
            var center = new Point(control.Bounds.Width / 4, 6);
            var point = control.TranslatePoint(center, app.Window);
            Assert.NotNull(point);
            Assert.InRange(point.Value.Y, 0, 31);
            app.Click(point.Value);
        }

        Assert.Equal(0, app.Window.MoveDragCount);
    }
}
