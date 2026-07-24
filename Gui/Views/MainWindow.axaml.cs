using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Gui.ViewModels;

namespace Gui.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
        PlaybackPositionSliderContainer.AddHandler(
            PointerPressedEvent,
            InputElement_OnPointerPressed,
            RoutingStrategies.Tunnel);

        PlaybackPositionSliderContainer.AddHandler(
            PointerReleasedEvent,
            InputElement_OnPointerReleased,
            RoutingStrategies.Tunnel);
        
        PlaybackPositionSliderContainer.AddHandler(
            PointerCaptureLostEvent,
            InputElement_OnPointerReleased,
            RoutingStrategies.Bubble);
    }

    private void InputElement_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if(DataContext is MainWindowVm vm)
        {
            vm.IsSeeking = true;
        }
    }

    private void InputElement_OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if(DataContext is MainWindowVm vm)
        {
            vm.IsSeeking = false;
        }
    }
}
