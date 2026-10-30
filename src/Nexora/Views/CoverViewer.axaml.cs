using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Nexora.Controls;
using Nexora.Render;
using Nexora.ViewModels;

namespace Nexora.Views;

public partial class CoverViewer : UserControl
{
    private CardTransform transform = new CardTransform();
    private Point? dragPosition;
    private TimeSpan? lastFrameTime;
    private bool isAnimationRunning;

    public CoverViewer()
    {
        InitializeComponent();
        Backdrop.PropertyChanged += OnBackdropPropertyChanged;
        Backdrop.PointerPressed += OnBackdropPointerPressed;
        Backdrop.PointerMoved += OnBackdropPointerMoved;
        Backdrop.PointerReleased += OnBackdropPointerReleased;
        Backdrop.PointerCaptureLost += OnBackdropPointerCaptureLost;
        Backdrop.PointerWheelChanged += OnBackdropPointerWheelChanged;
    }

    private void OnBackdropPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if(e.Property == PopupBackdrop.IsOpenProperty && Backdrop.IsOpen)
        {
            transform = new CardTransform();
            Cover.RenderTransform = null;
        }
        else if(e.Property == IsVisibleProperty && Backdrop.IsVisible)
        {
            CloseButton.Focus();
        }
        else if(e.Property == IsVisibleProperty && DataContext is CoverViewerVm vm)
        {
            vm.LargeCover.Release();
        }
    }

    private void OnBackdropPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if(e.GetCurrentPoint(Backdrop).Properties.IsLeftButtonPressed)
        {
            transform.Grab(GetSeconds(e));
            dragPosition = e.GetPosition(Backdrop);
            e.Pointer.Capture(Backdrop);
            e.Handled = true;
        }
    }

    private void OnBackdropPointerMoved(object? sender, PointerEventArgs e)
    {
        if(dragPosition == null)
        {
            return;
        }

        var position = e.GetPosition(Backdrop);
        transform.Drag(
            position.X - dragPosition.Value.X,
            position.Y - dragPosition.Value.Y,
            GetSeconds(e)
        );
        dragPosition = position;
        StartAnimation();
    }

    private void OnBackdropPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if(dragPosition == null)
        {
            return;
        }

        dragPosition = null;
        e.Pointer.Capture(null);
        transform.Release(GetSeconds(e));
        StartAnimation();
    }

    private void OnBackdropPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        if(dragPosition == null)
        {
            return;
        }

        dragPosition = null;
        transform.Release(double.PositiveInfinity);
        StartAnimation();
    }

    private void OnBackdropPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        transform.Zoom(e.Delta.Y);
        StartAnimation();
        e.Handled = true;
    }

    private static double GetSeconds(PointerEventArgs e)
    {
        return e.Timestamp / 1000.0;
    }

    private void StartAnimation()
    {
        var topLevel = TopLevel.GetTopLevel(this);

        if(isAnimationRunning || !transform.IsAnimating || topLevel == null)
        {
            return;
        }

        isAnimationRunning = true;
        lastFrameTime = null;
        topLevel.RequestAnimationFrame(OnAnimationFrame);
    }

    private void OnAnimationFrame(TimeSpan time)
    {
        var topLevel = TopLevel.GetTopLevel(this);

        if(!Backdrop.IsOpen || topLevel == null)
        {
            isAnimationRunning = false;
            return;
        }

        if(lastFrameTime != null && transform.Advance((time - lastFrameTime.Value).TotalSeconds))
        {
            UpdateTransform();
        }

        lastFrameTime = time;
        isAnimationRunning = transform.IsAnimating;

        if(isAnimationRunning)
        {
            topLevel.RequestAnimationFrame(OnAnimationFrame);
        }
    }

    private void UpdateTransform()
    {
        double cardSize = Math.Max(Cover.Bounds.Width, Cover.Bounds.Height);

        if(cardSize > 0)
        {
            Cover.RenderTransform = new MatrixTransform(transform.ToMatrix(cardSize));
        }
    }
}
