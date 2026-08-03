using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Nexora.ViewModels;

namespace Nexora.Controls;

public partial class PlaybackControl : UserControl
{
    public PlaybackControl()
    {
        InitializeComponent();
        
        
        VolumeSlider.AddHandler(
            PointerPressedEvent,
            VolumeSlider_OnPointerPressed,
            RoutingStrategies.Tunnel);
        
        VolumeSlider.AddHandler(
            PointerReleasedEvent,
            VolumeSlider_OnPointerReleased,
            RoutingStrategies.Tunnel);
        
        VolumeSlider.AddHandler(
            PointerCaptureLostEvent,
            VolumeSlider_OnPointerReleased,
            RoutingStrategies.Bubble);
        
        
        PlaybackPositionSlider.AddHandler(
            PointerPressedEvent,
            PlaybackPositionSlider_OnPointerPressed,
            RoutingStrategies.Tunnel);
        
        PlaybackPositionSlider.AddHandler(
            PointerReleasedEvent,
            PlaybackPositionSlider_OnPointerReleased,
            RoutingStrategies.Tunnel);
        
        PlaybackPositionSlider.AddHandler(
            PointerCaptureLostEvent,
            PlaybackPositionSlider_OnPointerReleased,
            RoutingStrategies.Bubble);
    }

    private void VolumeSlider_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if(DataContext is PlaybackVm vm)
        {
            vm.IsChangingVolume = true;
        }
    }

    private void VolumeSlider_OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if(DataContext is PlaybackVm vm)
        {
            vm.IsChangingVolume = false;
        }
    }

    private void PlaybackPositionSlider_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if(DataContext is PlaybackVm vm)
        {
            vm.IsSeeking = true;
        }
    }

    private void PlaybackPositionSlider_OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if(DataContext is PlaybackVm vm)
        {
            vm.IsSeeking = false;
        }
    }
}
