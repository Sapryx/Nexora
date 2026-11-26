using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Controls.Presenters;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
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
        var button = app.FindButton("window_maximize");

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
        app.Click(app.FindButton("window_maximize"));

        app.Click(app.FindButton("window_restore"));

        Assert.Equal(WindowState.Normal, app.Window.WindowState);
        Assert.Equal(size, app.Window.ClientSize);
        Assert.Equal(position, app.Window.Position);
        Assert.Contains(app.Window.GetVisualDescendants().OfType<Icon>(), it => it.IconName == "window_maximize");
    }

    [AvaloniaFact]
    public void WindowState_ChangedExternally_UpdatesMaximizeIcon()
    {
        using var app = new TestMainWindow();

        app.Window.WindowState = WindowState.Maximized;
        Dispatcher.UIThread.RunJobs();
        Assert.Contains(app.Window.GetVisualDescendants().OfType<Icon>(), it => it.IconName == "window_restore");
        Assert.DoesNotContain(app.Window.GetVisualDescendants().OfType<Icon>(), it => it.IconName == "window_maximize");

        app.Window.WindowState = WindowState.Normal;
        Dispatcher.UIThread.RunJobs();
        Assert.Contains(app.Window.GetVisualDescendants().OfType<Icon>(), it => it.IconName == "window_maximize");
        Assert.DoesNotContain(app.Window.GetVisualDescendants().OfType<Icon>(), it => it.IconName == "window_restore");
    }

    [AvaloniaFact]
    public void CloseButton_Click_ClosesWindow()
    {
        using var app = new TestMainWindow();
        bool closed = false;
        app.Window.Closed += (_, _) => closed = true;

        app.Click(app.FindButton("window_close"));

        Assert.True(closed);
    }

    [AvaloniaFact]
    public void CaptionButtons_Hover_HighlightAndResetOnLeave()
    {
        using var app = new TestMainWindow();
        var minimize = app.FindButton("window_minimize");
        var maximize = app.FindButton("window_maximize");
        var close = app.FindButton("window_close");

        app.Hover(minimize);
        Assert.Equal(Color.Parse("#1AFFFFFF"), BackgroundOf(minimize));
        Assert.Equal(Colors.Transparent, BackgroundOf(maximize));

        app.Hover(maximize);
        Assert.Equal(Color.Parse("#1AFFFFFF"), BackgroundOf(maximize));
        Assert.Equal(Colors.Transparent, BackgroundOf(minimize));

        app.Hover(close);
        Assert.Equal(Color.Parse("#E81123"), BackgroundOf(close));

        app.Window.MouseMove(new Point(5, 400));
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(Colors.Transparent, BackgroundOf(close));
        Assert.Equal(Colors.Transparent, BackgroundOf(maximize));
    }

    private static Color BackgroundOf(Button button)
    {
        var presenter = button.GetVisualDescendants().OfType<ContentPresenter>().First();
        return ((ISolidColorBrush?)presenter.Background)?.Color ?? Colors.Transparent;
    }
}
