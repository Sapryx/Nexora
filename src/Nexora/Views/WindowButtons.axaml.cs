using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace Nexora.Views;

public partial class WindowButtons : UserControl
{
    private const string MaximizeIconName = "window_maximize";
    private const string RestoreIconName = "window_restore";
    private Window? observedWindow;

    public WindowButtons()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        if(TopLevel.GetTopLevel(this) is Window window)
        {
            observedWindow = window;
            window.PropertyChanged += OnWindowPropertyChanged;
            UpdateMaximizeIcon(window.WindowState);
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if(observedWindow != null)
        {
            observedWindow.PropertyChanged -= OnWindowPropertyChanged;
            observedWindow = null;
        }

        base.OnDetachedFromVisualTree(e);
    }

    private void OnWindowPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if(e.Property == Window.WindowStateProperty)
        {
            UpdateMaximizeIcon((WindowState)e.NewValue!);
        }
    }

    private void UpdateMaximizeIcon(WindowState state)
    {
        MaximizeIcon.IconName = state == WindowState.Maximized ? RestoreIconName : MaximizeIconName;
    }

    private void OnMinimizeClick(object? sender, RoutedEventArgs e)
    {
        if(TopLevel.GetTopLevel(this) is Window window)
        {
            window.WindowState = WindowState.Minimized;
        }
    }

    private void OnMaximizeClick(object? sender, RoutedEventArgs e)
    {
        if(TopLevel.GetTopLevel(this) is Window window)
        {
            window.WindowState = window.WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        }
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        if(TopLevel.GetTopLevel(this) is Window window)
        {
            window.Close();
        }
    }
}
