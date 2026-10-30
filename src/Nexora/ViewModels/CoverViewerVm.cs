using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nexora.Media;
using Nexora.Threading;

namespace Nexora.ViewModels;

public partial class CoverViewerVm : ViewModelBase
{
    [ObservableProperty]
    public partial bool IsOpen { get; private set; }

    public LargeCoverVm LargeCover { get; }

    public CoverViewerVm(ICoverCache coverCache, IUiDispatcher uiDispatcher)
    {
        LargeCover = new LargeCoverVm(coverCache.LoadOriginalAsync, uiDispatcher);
    }

    public void Open(string audioPath, Bitmap? preview)
    {
        LargeCover.Load(audioPath, preview);
        IsOpen = true;
    }

    [RelayCommand]
    public void Close()
    {
        IsOpen = false;
    }
}
