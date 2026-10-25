using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Nexora.ViewModels;

namespace Nexora.Controls;

public partial class TrackGrid : UserControl
{
    public static readonly StyledProperty<ObservableCollection<TrackControlVm>?> TrackVmsProperty =
        AvaloniaProperty.Register<TrackGrid, ObservableCollection<TrackControlVm>?>(nameof(TrackVms));

    public ObservableCollection<TrackControlVm>? TrackVms
    {
        get => GetValue(TrackVmsProperty);
        set => SetValue(TrackVmsProperty, value);
    }

    public static readonly StyledProperty<int> ColumnCountProperty =
        AvaloniaProperty.Register<TrackGrid, int>(nameof(ColumnCount), 1);

    public int ColumnCount
    {
        get => GetValue(ColumnCountProperty);
        set => SetValue(ColumnCountProperty, value);
    }

    public ObservableCollection<TrackGridRow> Rows { get; } = [];

    private const double CellWidth = 192;
    private const double RightInset = 32;
    private bool isUpdatePending;

    public TrackGrid()
    {
        InitializeComponent();
        new SmoothScroller(RowScrollViewer, RowList);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if(change.Property == TrackVmsProperty)
        {
            if(change.OldValue is ObservableCollection<TrackControlVm> oldTrackVms)
            {
                oldTrackVms.CollectionChanged -= OnTrackVmsChanged;
            }

            if(change.NewValue is ObservableCollection<TrackControlVm> newTrackVms)
            {
                newTrackVms.CollectionChanged += OnTrackVmsChanged;
            }

            ScheduleRowsUpdate();
        }
        else if(change.Property == ColumnCountProperty)
        {
            ScheduleRowsUpdate();
        }
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        ColumnCount = Math.Max(1, (int)((e.NewSize.Width - RightInset) / CellWidth));
    }

    private void OnTrackVmsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        ScheduleRowsUpdate();
    }

    // The search refills the collection one item at a time, so rows are rebuilt once after the whole batch.
    private void ScheduleRowsUpdate()
    {
        if(isUpdatePending)
        {
            return;
        }

        isUpdatePending = true;
        Dispatcher.UIThread.Post(() =>
        {
            isUpdatePending = false;
            TrackGridRow.UpdateRows(Rows, TrackVms ?? [], ColumnCount);
        }, DispatcherPriority.Background);
    }
}
