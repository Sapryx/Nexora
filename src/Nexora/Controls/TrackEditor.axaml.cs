using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Nexora.ViewModels;

namespace Nexora.Controls;

public partial class TrackEditor : UserControl
{
    private bool IsOpen => Backdrop.Tag is true;
    private TimeSpan CloseDuration =>
        this.FindResource("PopupCloseDuration") is TimeSpan duration ? duration : TimeSpan.Zero;

    public TrackEditor()
    {
        InitializeComponent();
        ArtistInput.AddHandler(KeyDownEvent, OnArtistInputKeyDown, RoutingStrategies.Tunnel);
        Backdrop.PropertyChanged += OnBackdropPropertyChanged;
        Backdrop.PointerPressed += OnBackdropPointerPressed;
    }

    private void OnBackdropPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if(e.Source == Backdrop && DataContext is TrackEditorVm vm)
        {
            vm.Cancel();
            e.Handled = true;
        }
    }

    private void OnBackdropPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if(e.Property == TagProperty)
        {
            if(IsOpen)
            {
                Backdrop.IsVisible = true;
            }
            else
            {
                DispatcherTimer.RunOnce(() => Backdrop.IsVisible = IsOpen, CloseDuration);
            }
        }
        else if(e.Property == IsVisibleProperty && Backdrop.IsVisible)
        {
            TitleBox.Focus();
        }
    }

    private void OnArtistInputKeyDown(object? sender, KeyEventArgs e)
    {
        if(DataContext is not TrackEditorVm vm)
        {
            return;
        }

        switch(e.Key)
        {
            case Key.Down:
                vm.MoveSuggestionSelection(1);
                e.Handled = true;
                break;
            case Key.Up:
                vm.MoveSuggestionSelection(-1);
                e.Handled = true;
                break;
            case Key.Enter:
                vm.AddSelectedSuggestion();
                e.Handled = true;
                break;
            case Key.Back:
                vm.RemoveLastArtist();
                break;
        }
    }

    private void OnSuggestionTapped(object? sender, TappedEventArgs e)
    {
        if(DataContext is TrackEditorVm vm)
        {
            vm.AddSelectedSuggestion();
            ArtistInput.Focus();
        }
    }

    private void OnRemoveArtistClick(object? sender, RoutedEventArgs e)
    {
        if(sender is Button { DataContext: string artist } && DataContext is TrackEditorVm vm)
        {
            vm.RemoveArtist(artist);
        }
    }
}
