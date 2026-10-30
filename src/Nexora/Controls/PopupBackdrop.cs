using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;

namespace Nexora.Controls;

public class PopupBackdrop : Panel
{
    public static readonly StyledProperty<bool> IsOpenProperty =
        AvaloniaProperty.Register<PopupBackdrop, bool>(nameof(IsOpen));

    public bool IsOpen
    {
        get => GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    private TimeSpan CloseDuration =>
        this.FindResource("PopupCloseDuration") is TimeSpan duration ? duration : TimeSpan.Zero;

    public PopupBackdrop()
    {
        IsVisible = false;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if(change.Property != IsOpenProperty)
        {
            return;
        }

        if(IsOpen)
        {
            IsVisible = true;
        }
        else
        {
            DispatcherTimer.RunOnce(() => IsVisible = IsOpen, CloseDuration);
        }

        Classes.Set("Opening", IsOpen);
        Classes.Set("Open", IsOpen);
    }
}
