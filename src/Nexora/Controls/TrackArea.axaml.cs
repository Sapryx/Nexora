using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Nexora.ViewModels;

namespace Nexora.Controls;

public partial class TrackArea : UserControl
{
    public static readonly StyledProperty<ObservableCollection<AudioTrackVm>> TrackVmsProperty =
        AvaloniaProperty.Register<TrackArea, ObservableCollection<AudioTrackVm>>(nameof(TrackVms)
    );

    public ObservableCollection<AudioTrackVm> TrackVms
    {
        get => GetValue(TrackVmsProperty);
        set => SetValue(TrackVmsProperty, value);
    }

    public TrackArea()
    {
        InitializeComponent();
    }
}
