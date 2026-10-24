using Avalonia.Controls;
using Avalonia.Interactivity;
using Nexora.ViewModels;

namespace Nexora.Controls;

public partial class TrackEditor : UserControl
{
    public TrackEditor()
    {
        InitializeComponent();
    }

    private void OnFieldLostFocus(object? sender, RoutedEventArgs e)
    {
        if(DataContext is TrackEditorVm vm)
        {
            _ = vm.ApplyAsync();
        }
    }
}
