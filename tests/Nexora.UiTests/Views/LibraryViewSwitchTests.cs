using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Nexora.Controls;
using Nexora.Views;

namespace Nexora.UiTests.Views;

public class LibraryViewSwitchTests
{
    [AvaloniaFact]
    public void Switch_IsRightNextToSearchBar()
    {
        using var app = new TestMainWindow();

        var searchBounds = BoundsInWindow(app, app.Window.GetVisualDescendants().OfType<SearchBar>().Single());
        var switchBounds = BoundsInWindow(app, FindSwitch(app));

        Assert.InRange(switchBounds.Left - searchBounds.Right, 0, 24);
    }

    [AvaloniaTheory]
    [InlineData(1280)]
    [InlineData(640)]
    public void Switch_DoesNotOverlapWindowButtons(double width)
    {
        using var app = new TestMainWindow();
        app.Window.Width = width;
        Dispatcher.UIThread.RunJobs();

        var switchBounds = BoundsInWindow(app, FindSwitch(app));
        var buttonsBounds = BoundsInWindow(app, app.Window.GetVisualDescendants().OfType<WindowButtons>().Single());

        Assert.False(switchBounds.Intersects(buttonsBounds));
    }

    private static SegmentedControl FindSwitch(TestMainWindow app)
    {
        return app.Window
            .GetVisualDescendants()
            .OfType<SegmentedControl>()
            .Single(it => it.Name == "LibraryViewSwitch");
    }

    private static Rect BoundsInWindow(TestMainWindow app, Control control)
    {
        var origin = control.TranslatePoint(default, app.Window)
            ?? throw new InvalidOperationException($"{control} is not in the main window");
        return new Rect(origin, control.Bounds.Size);
    }
}
