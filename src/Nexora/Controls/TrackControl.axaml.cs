using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Nexora.ViewModels;

namespace Nexora.Controls;

public partial class AudioTrackView : UserControl
{
    public AudioTrackView()
    {
        InitializeComponent();
        MainTrackBorder.AddHandler(PointerPressedEvent, OnTrackPointerPressed, RoutingStrategies.Bubble, true);
    }

    private void OnTrackPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if(e.Source is Visual source
           && !source.GetSelfAndVisualAncestors().TakeWhile(it => it != MainTrackBorder).OfType<Button>().Any()
           && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed
           && DataContext is TrackControlVm trackVm)
        {
            trackVm.EditCommand.Execute(null);
        }
    }
}
