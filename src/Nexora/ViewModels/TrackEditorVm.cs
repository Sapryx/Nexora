using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Nexora.ViewModels;

public partial class TrackEditorVm : ViewModelBase
{
    [ObservableProperty]
    public partial TrackControlVm? TrackVm { get; private set; }

    public void Open(TrackControlVm trackVm)
    {
        if(TrackVm == trackVm)
        {
            return;
        }

        _ = ApplyAsync();
        trackVm.BeginEdit();
        TrackVm = trackVm;
    }

    [RelayCommand]
    public void Close()
    {
        _ = ApplyAsync();
        TrackVm = null;
    }

    public Task ApplyAsync()
    {
        return TrackVm?.ApplyEditAsync() ?? Task.CompletedTask;
    }
}
