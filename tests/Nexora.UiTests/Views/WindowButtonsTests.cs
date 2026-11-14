using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Nexora.Controls;

namespace Nexora.UiTests.Views;

public class WindowButtonsTests
{
    [AvaloniaFact]
    public void MinimizeButton_Click_MinimizesWindow()
    {
        using var app = new TestMainWindow();
        var minimizeButton = app.Window
            .GetVisualDescendants()
            .OfType<Icon>()
            .Single(it => it.IconName == "window_minimize")
            .FindAncestorOfType<Button>();
        Assert.NotNull(minimizeButton);

        app.Click(minimizeButton);

        Assert.Equal(WindowState.Minimized, app.Window.WindowState);
    }

    [AvaloniaFact]
    public void MaximizeButton_Click_MaximizesWindowAndShowsRestoreIcon()
    {
        using var app = new TestMainWindow();
        var button = FindButton(app, "window_maximize");

        app.Click(button);

        Assert.Equal(WindowState.Maximized, app.Window.WindowState);
        Assert.Contains(app.Window.GetVisualDescendants().OfType<Icon>(), it => it.IconName == "window_restore");
    }

    [AvaloniaFact]
    public void RestoreButton_Click_RestoresWindowAndShowsMaximizeIcon()
    {
        using var app = new TestMainWindow();
        var size = app.Window.ClientSize;
        var position = app.Window.Position;
        app.Click(FindButton(app, "window_maximize"));

        app.Click(FindButton(app, "window_restore"));

        Assert.Equal(WindowState.Normal, app.Window.WindowState);
        Assert.Equal(size, app.Window.ClientSize);
        Assert.Equal(position, app.Window.Position);
        Assert.Contains(app.Window.GetVisualDescendants().OfType<Icon>(), it => it.IconName == "window_maximize");
    }

    [AvaloniaFact]
    public void CloseButton_Click_ClosesWindow()
    {
        using var app = new TestMainWindow();
        bool closed = false;
        app.Window.Closed += (_, _) => closed = true;

        app.Click(FindButton(app, "window_close"));

        Assert.True(closed);
    }

    private static Button FindButton(TestMainWindow app, string iconName)
    {
        var button = app.Window
            .GetVisualDescendants()
            .OfType<Icon>()
            .Single(it => it.IconName == iconName)
            .FindAncestorOfType<Button>();
        Assert.NotNull(button);
        return button;
    }
}
