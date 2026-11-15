using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Nexora.Controls;
using Nexora.ViewModels;

namespace Nexora.Views;

public partial class MainWindow : Window
{
    private bool IsPopupOpen => DataContext is MainWindowVm vm && vm.IsPopupOpen;
    private const double DragStripHeight = 32;
    private const double ResizeBorderThickness = 6;
    private WindowEdge? hoveredEdge;
    private WindowState stateBeforeFullScreen = WindowState.Normal;

    private readonly Dictionary<Key, Action<MainWindowVm>> keyActions = new Dictionary<Key, Action<MainWindowVm>>()
    {
        {
            Key.Space, vm => vm.PlaybackVm.Pause()
        },
        {
            Key.Left, vm => vm.PlaybackVm.SkipBack()
        },
        {
            Key.Right, vm => vm.PlaybackVm.SkipForward()
        }
    };

    public MainWindow()
    {
        InitializeComponent();

        AddHandler(KeyDownEvent, OnPreviewKey, RoutingStrategies.Tunnel);
        AddHandler(KeyUpEvent, OnPreviewKey, RoutingStrategies.Tunnel);
        AddHandler(PointerPressedEvent, OnPreviewPointerPressed, RoutingStrategies.Tunnel);
        AddHandler(PointerMovedEvent, OnPreviewPointerMoved, RoutingStrategies.Tunnel);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        bool isLeftPress = e.GetCurrentPoint(this).Properties.IsLeftButtonPressed;

        if(e.Handled || !isLeftPress || e.GetPosition(this).Y >= DragStripHeight || IsOnInteractiveControl(e))
        {
            return;
        }

        if(e.ClickCount == 2)
        {
            this.ToggleMaximize();
            return;
        }

        StartMoveDrag(e);
    }

    private bool IsOnInteractiveControl(PointerPressedEventArgs e)
    {
        return e.Source is Visual source
            && source.GetSelfAndVisualAncestors()
                .Any(visual => visual is SearchBar or SegmentedControl or WindowButtons);
    }

    protected virtual void StartMoveDrag(PointerPressedEventArgs e)
    {
        BeginMoveDrag(e);
    }

    protected virtual void StartResizeDrag(WindowEdge edge, PointerPressedEventArgs e)
    {
        BeginResizeDrag(edge, e);
    }

    private WindowEdge? ResizeEdgeAt(PointerEventArgs e)
    {
        if(WindowState != WindowState.Normal)
        {
            return null;
        }

        return ResizeEdges.Find(e.GetPosition(this), Bounds.Size, ResizeBorderThickness);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if(e.Handled || e.Source is TextBox || IsPopupOpen || DataContext is MainWindowVm { IsSettingsOpen: true })
        {
            return;
        }

        bool isScrolled = LibraryGrid.IsVisible ? LibraryGrid.ScrollByKey(e.Key) : LibraryList.ScrollByKey(e.Key);

        if(isScrolled)
        {
            e.Handled = true;
        }
    }

    private bool TryStartResizeDrag(PointerPressedEventArgs e)
    {
        var edge = ResizeEdgeAt(e);

        if(edge == null || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return false;
        }

        StartResizeDrag(edge.Value, e);
        e.Handled = true;
        return true;
    }

    private void OnPreviewPointerMoved(object? sender, PointerEventArgs e)
    {
        var edge = ResizeEdgeAt(e);

        if(edge == hoveredEdge)
        {
            return;
        }

        hoveredEdge = edge;
        Cursor = edge == null ? null : new Cursor(ResizeEdges.CursorOf(edge.Value));
    }

    private void OnPreviewPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if(!TryStartResizeDrag(e))
        {
            UnfocusTextBoxOnOutsidePress(e);
        }
    }

    private void UnfocusTextBoxOnOutsidePress(PointerPressedEventArgs e)
    {
        if(FocusManager?.GetFocusedElement() is not TextBox textBox)
        {
            return;
        }

        if(e.Source is Visual source && textBox.IsVisualAncestorOf(source))
        {
            return;
        }

        Focus();
    }

    private void ToggleFullScreen()
    {
        if(WindowState == WindowState.FullScreen)
        {
            WindowState = stateBeforeFullScreen;
            return;
        }

        stateBeforeFullScreen = WindowState;
        WindowState = WindowState.FullScreen;
    }

    private void OnPreviewKey(object? sender, KeyEventArgs e)
    {
        if(e.Key == Key.F11)
        {
            if(e.RoutedEvent == KeyDownEvent)
            {
                ToggleFullScreen();
            }

            e.Handled = true;
            return;
        }

        if(DataContext is MainWindowVm mainWindowVm && e.Key == Key.Escape && IsPopupOpen)
        {
            if(e.RoutedEvent == KeyDownEvent)
            {
                mainWindowVm.ClosePopup();
            }

            e.Handled = true;
            return;
        }

        if(e.Source is TextBox || IsPopupOpen)
        {
            return;
        }

        if(DataContext is not MainWindowVm vm || !keyActions.TryGetValue(e.Key, out var action))
        {
            return;
        }

        if(e.RoutedEvent == KeyDownEvent)
        {
            action(vm);
        }

        e.Handled = true;
    }
}
