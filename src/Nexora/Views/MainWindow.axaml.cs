using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
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

    public MainWindow()
    {
        InitializeComponent();

        AddHandler(KeyDownEvent, OnPreviewKey, RoutingStrategies.Tunnel);
        AddHandler(KeyUpEvent, OnPreviewKey, RoutingStrategies.Tunnel);
    }

    private void OnPreviewKey(object? sender, KeyEventArgs e)
    {
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
