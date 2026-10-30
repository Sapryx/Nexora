using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Nexora.ViewModels;

namespace Nexora.Views;

public partial class MainWindow : Window
{
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

    private bool IsTrackEditorOpen => DataContext is MainWindowVm vm && vm.TrackEditorVm.IsOpen;

    public MainWindow()
    {
        InitializeComponent();

        AddHandler(KeyDownEvent, OnPreviewKey, RoutingStrategies.Tunnel);
        AddHandler(KeyUpEvent, OnPreviewKey, RoutingStrategies.Tunnel);
        AddHandler(PointerPressedEvent, OnPreviewPointerPressed, RoutingStrategies.Tunnel);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if(e.Handled || e.Source is TextBox || IsTrackEditorOpen ||
           DataContext is MainWindowVm { IsSettingsOpen: true })
        {
            return;
        }

        bool isScrolled = LibraryGrid.IsVisible ? LibraryGrid.ScrollByKey(e.Key) : LibraryList.ScrollByKey(e.Key);

        if(isScrolled)
        {
            e.Handled = true;
        }
    }

    private void OnPreviewPointerPressed(object? sender, PointerPressedEventArgs e)
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

    private void OnPreviewKey(object? sender, KeyEventArgs e)
    {
        if(DataContext is MainWindowVm mainWindowVm && e.Key == Key.Escape && IsTrackEditorOpen)
        {
            if(e.RoutedEvent == KeyDownEvent)
            {
                mainWindowVm.TrackEditorVm.Cancel();
            }

            e.Handled = true;
            return;
        }

        if(e.Source is TextBox || IsTrackEditorOpen)
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
