using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Nexora.Controls;
using Nexora.Media;
using Nexora.ViewModels;

namespace Nexora.Views;

public partial class TrackArea : UserControl
{
    public static readonly StyledProperty<ObservableCollection<TrackControlVm>> TrackVmsProperty =
        AvaloniaProperty.Register<TrackArea, ObservableCollection<TrackControlVm>>(nameof(TrackVms));

    public ObservableCollection<TrackControlVm> TrackVms
    {
        get => GetValue(TrackVmsProperty);
        set => SetValue(TrackVmsProperty, value);
    }

    public static readonly StyledProperty<int> FirstVisibleTrackIndexProperty =
        AvaloniaProperty.Register<TrackArea, int>(
            nameof(FirstVisibleTrackIndex),
            defaultBindingMode: BindingMode.TwoWay
        );

    public int FirstVisibleTrackIndex
    {
        get => GetValue(FirstVisibleTrackIndexProperty);
        set => SetValue(FirstVisibleTrackIndexProperty, value);
    }

    public static readonly StyledProperty<CoverViewport?> CoversProperty =
        AvaloniaProperty.Register<TrackArea, CoverViewport?>(nameof(Covers));

    public CoverViewport? Covers => GetValue(CoversProperty);
    private SmoothScroller? scroller;

    public TrackArea()
    {
        InitializeComponent();
        TrackList.TemplateApplied += OnTrackListTemplateApplied;
    }

    private void OnTrackListTemplateApplied(object? sender, TemplateAppliedEventArgs e)
    {
        var scrollViewer = e.NameScope.Find<ScrollViewer>("PART_ScrollViewer");

        if(scrollViewer != null)
        {
            scroller = new SmoothScroller(scrollViewer, TrackList);
            scrollViewer.ScrollChanged += OnScrollChanged;
            scrollViewer.LayoutUpdated += (_, _) => ReportCovers();
        }
    }

    public bool ScrollByKey(Key key)
    {
        return scroller?.ScrollByKey(key) == true;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if(change.Property == IsVisibleProperty)
        {
            OnVisibilityChanged();
        }
    }

    private void OnVisibilityChanged()
    {
        ReportCovers();

        if(IsVisible)
        {
            scroller?.ScrollToItem(FirstVisibleTrackIndex);
        }
    }

    private void ReportCovers()
    {
        if(scroller != null)
        {
            Covers?.Show(IsVisible ? scroller.VisibleItemCount : 0, CoverSizes.Thumbnail);
        }
    }

    private void OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if(IsVisible && scroller != null)
        {
            FirstVisibleTrackIndex = scroller.FirstVisibleItemIndex;
        }
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if(sender is ListBox listBox)
        {
            listBox.SelectedItem = null;
        }
    }
}
