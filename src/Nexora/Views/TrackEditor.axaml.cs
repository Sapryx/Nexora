using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Threading;
using Nexora.ViewModels;

namespace Nexora.Views;

public partial class TrackEditor : UserControl
{
    private bool IsOpen => Backdrop.Tag is true;
    private TimeSpan CloseDuration =>
        this.FindResource("PopupCloseDuration") is TimeSpan duration ? duration : TimeSpan.Zero;
    private const double ChipDragThreshold = 4;
    private const double DraggedChipOpacity = 0.4;
    private TrackEditorVm? vm;
    private Control? draggedChip;
    private Point chipDragStart;
    private int chipDragStartIndex;
    private bool isDraggingChip;

    public TrackEditor()
    {
        InitializeComponent();
        ArtistInput.AddHandler(KeyDownEvent, OnArtistInputKeyDown, RoutingStrategies.Tunnel);
        Backdrop.PropertyChanged += OnBackdropPropertyChanged;
        Backdrop.PointerPressed += OnBackdropPointerPressed;
        ArtistsPanel.PointerMoved += OnArtistsPanelPointerMoved;
        ArtistsPanel.PointerReleased += OnArtistsPanelPointerReleased;
        ArtistsPanel.PointerCaptureLost += OnArtistsPanelPointerCaptureLost;
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if(vm != null)
        {
            vm.EditedArtists.CollectionChanged -= OnEditedArtistsChanged;
        }

        vm = DataContext as TrackEditorVm;

        if(vm != null)
        {
            vm.EditedArtists.CollectionChanged += OnEditedArtistsChanged;
        }

        UpdateArtistChips();
    }

    private void OnEditedArtistsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdateArtistChips();
    }

    private void UpdateArtistChips()
    {
        ArtistsPanel.Children.RemoveAll(ArtistsPanel.Children.Where(it => it != ArtistInput).ToList());

        if(vm == null || this.FindResource("ArtistChipTemplate") is not DataTemplate template)
        {
            return;
        }

        foreach(string artist in vm.EditedArtists)
        {
            var chip = template.Build(artist);

            if(chip != null)
            {
                chip.DataContext = artist;
                chip.PointerPressed += OnChipPointerPressed;
                ArtistsPanel.Children.Insert(ArtistsPanel.Children.Count - 1, chip);
            }
        }
    }

    private void OnChipPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if(sender is Control chip && e.GetCurrentPoint(chip).Properties.IsLeftButtonPressed)
        {
            draggedChip = chip;
            chipDragStart = e.GetPosition(ArtistsPanel);
            chipDragStartIndex = ArtistsPanel.Children.IndexOf(chip);
            e.Pointer.Capture(ArtistsPanel);
            e.Handled = true;
        }
    }

    private void OnArtistsPanelPointerMoved(object? sender, PointerEventArgs e)
    {
        if(draggedChip == null)
        {
            return;
        }

        var position = e.GetPosition(ArtistsPanel);

        if(!isDraggingChip)
        {
            var offset = position - chipDragStart;

            if(Math.Abs(offset.X) < ChipDragThreshold && Math.Abs(offset.Y) < ChipDragThreshold)
            {
                return;
            }

            isDraggingChip = true;
            draggedChip.Opacity = DraggedChipOpacity;
        }

        int index = ArtistsPanel.Children.IndexOf(draggedChip);
        int dropIndex = ArtistsPanel.Children
            .Where(it => it != draggedChip && it != ArtistInput)
            .Count(it => IsBefore(it.Bounds, position));

        if(dropIndex != index)
        {
            ArtistsPanel.Children.Move(index, dropIndex);
        }
    }

    private void OnArtistsPanelPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if(draggedChip == null)
        {
            return;
        }

        int dropIndex = ArtistsPanel.Children.IndexOf(draggedChip);
        draggedChip.Opacity = 1;
        draggedChip = null;
        isDraggingChip = false;
        e.Pointer.Capture(null);

        if(dropIndex != chipDragStartIndex)
        {
            vm?.MoveArtist(chipDragStartIndex, dropIndex);
        }
    }

    private void OnArtistsPanelPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        if(draggedChip != null)
        {
            draggedChip = null;
            isDraggingChip = false;
            UpdateArtistChips();
        }
    }

    private static bool IsBefore(Rect bounds, Point position)
    {
        return position.Y > bounds.Bottom || (position.Y >= bounds.Top && position.X > bounds.Center.X);
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
                TitleBox.ClearSelection();
                TitleBox.CaretIndex = TitleBox.Text?.Length ?? 0;
            }
            else
            {
                DispatcherTimer.RunOnce(() => Backdrop.IsVisible = IsOpen, CloseDuration);
            }

            Backdrop.Classes.Set("Opening", IsOpen);
            Backdrop.Classes.Set("Open", IsOpen);
        }
        else if(e.Property == IsVisibleProperty && Backdrop.IsVisible)
        {
            TitleBox.Focus();
        }
        else if(e.Property == IsVisibleProperty)
        {
            vm?.ReleaseLargeCover();
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

    private async void OnCopyArtistClick(object? sender, RoutedEventArgs e)
    {
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;

        if(sender is MenuItem { DataContext: string artist } && clipboard != null)
        {
            await clipboard.SetTextAsync(artist);
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
