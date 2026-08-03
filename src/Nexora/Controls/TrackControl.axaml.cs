using Avalonia;
using Avalonia.Controls;

namespace Nexora.Controls;

public partial class AudioTrackView : UserControl
{
    public static readonly StyledProperty<bool> IsInteractiveProperty =
        AvaloniaProperty.Register<AudioTrackView, bool>(nameof(IsInteractive), true);

    public static readonly StyledProperty<bool> ShowDurationProperty =
        AvaloniaProperty.Register<AudioTrackView, bool>(nameof(ShowDuration), true);

    public bool IsInteractive
    {
        get => GetValue(IsInteractiveProperty);
        set => SetValue(IsInteractiveProperty, value);
    }

    public bool ShowDuration
    {
        get => GetValue(ShowDurationProperty);
        set => SetValue(ShowDurationProperty, value);
    }
    
    public AudioTrackView()
    {
        InitializeComponent();
    }
}
