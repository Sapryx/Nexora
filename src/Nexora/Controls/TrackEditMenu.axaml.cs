using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Nexora.ViewModels;

namespace Nexora.Controls;

public partial class TrackEditMenu : UserControl
{
    private static readonly TimeSpan AnimationDuration = TimeSpan.FromMilliseconds(200);
    private readonly TranslateTransform editorPanelTranslate = new TranslateTransform();
    private TopLevel? topLevel;
    private ScrollViewer? scrollViewer;
    private CancellationTokenSource? animationCancellation;
    private TrackControlVm? editedVm;
    private bool isOpen;

    public TrackEditMenu()
    {
        InitializeComponent();
        EditorPanel.RenderTransform = editorPanelTranslate;

        // Popup content routes events to the popup's logical parent, the track: a focused text box would scroll
        // the track list to bring itself into view (which closes the panel), and clicks would reach the track.
        EditorPanel.AddHandler(RequestBringIntoViewEvent, OnEditorPanelEvent);
        EditorPanel.AddHandler(PointerPressedEvent, OnEditorPanelEvent);
        EditorPanel.AddHandler(ContextRequestedEvent, OnEditorPanelEvent);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        Close();
        EditorPopup.IsOpen = false;
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        Close();
    }

    private void OnMenuButtonClick(object? sender, RoutedEventArgs e)
    {
        if(isOpen)
        {
            Close();
        }
        else
        {
            Open();
        }
    }

    private void Open()
    {
        if(DataContext is not TrackControlVm vm)
        {
            return;
        }

        bool isCollapsing = EditorPopup.IsOpen;
        editedVm = vm;
        vm.BeginEdit();
        isOpen = true;
        PseudoClasses.Set(":open", true);
        EditorPopup.IsOpen = true;
        Subscribe();

        EditorPanel.Measure(Size.Infinity);
        double from = isCollapsing ? editorPanelTranslate.Y : -EditorPanel.DesiredSize.Height;
        _ = AnimateAsync(from, 0);
    }

    private void Close()
    {
        if(!isOpen)
        {
            return;
        }

        isOpen = false;
        PseudoClasses.Set(":open", false);
        Unsubscribe();
        _ = editedVm?.ApplyEditAsync();
        editedVm = null;
        _ = CollapseAsync();
    }

    private async Task CollapseAsync()
    {
        await AnimateAsync(editorPanelTranslate.Y, -EditorPanel.Bounds.Height);

        if(!isOpen)
        {
            EditorPopup.IsOpen = false;
        }
    }

    private Task AnimateAsync(double from, double to)
    {
        animationCancellation?.Cancel();
        animationCancellation = new CancellationTokenSource();
        editorPanelTranslate.Y = from;

        var animation = new Animation()
        {
            Duration = AnimationDuration,
            Easing = new CubicEaseOut(),
            FillMode = FillMode.Forward,
            Children =
            {
                new KeyFrame() { Cue = new Cue(0), Setters = { new Setter(TranslateTransform.YProperty, from) } },
                new KeyFrame() { Cue = new Cue(1), Setters = { new Setter(TranslateTransform.YProperty, to) } }
            }
        };

        return animation.RunAsync(EditorPanel, animationCancellation.Token);
    }

    private void Subscribe()
    {
        topLevel = TopLevel.GetTopLevel(this);
        scrollViewer = this.FindAncestorOfType<ScrollViewer>();

        topLevel?.AddHandler(PointerPressedEvent, OnTopLevelPointerPressed, RoutingStrategies.Tunnel, true);

        if(topLevel is Window window)
        {
            window.Deactivated += OnWindowDeactivated;
        }

        if(scrollViewer != null)
        {
            scrollViewer.ScrollChanged += OnScrollChanged;
        }
    }

    private void Unsubscribe()
    {
        topLevel?.RemoveHandler(PointerPressedEvent, OnTopLevelPointerPressed);

        if(topLevel is Window window)
        {
            window.Deactivated -= OnWindowDeactivated;
        }

        if(scrollViewer != null)
        {
            scrollViewer.ScrollChanged -= OnScrollChanged;
        }

        topLevel = null;
        scrollViewer = null;
    }

    private void OnTopLevelPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if(e.Source is not Visual source || IsWithin(source, EditorPanel) || IsWithin(source, MenuButton))
        {
            return;
        }

        Close();
    }

    private static bool IsWithin(Visual source, Visual container)
    {
        return source == container || container.IsVisualAncestorOf(source);
    }

    private void OnEditorPanelEvent(object? sender, RoutedEventArgs e)
    {
        e.Handled = true;
    }

    private void OnWindowDeactivated(object? sender, EventArgs e)
    {
        Close();
    }

    private void OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if(e.OffsetDelta.Y != 0)
        {
            Close();
        }
    }
}
