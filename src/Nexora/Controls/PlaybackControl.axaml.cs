using Avalonia.Controls;
using Avalonia.Input;
using Nexora.ViewModels;

namespace Nexora.Controls;

public partial class PlaybackControl : UserControl
{
    public PlaybackControl()
    {
        InitializeComponent();
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
}
