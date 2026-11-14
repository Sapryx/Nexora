using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Nexora.Views;

public partial class WindowButtons : UserControl
{
    public WindowButtons()
    {
        InitializeComponent();
    }

    private void OnMinimizeClick(object? sender, RoutedEventArgs e)
    {
        if(TopLevel.GetTopLevel(this) is Window window)
        {
            window.WindowState = WindowState.Minimized;
        }
    }
}
