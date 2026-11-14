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
}
