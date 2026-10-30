using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Nexora.Threading;

namespace Nexora.ViewModels;

public partial class LargeCoverVm : ViewModelBase
{
    [ObservableProperty]
    public partial Bitmap? Cover { get; private set; }

    private readonly Func<string, Task<Bitmap?>> loadCover;
    private readonly IUiDispatcher uiDispatcher;
    private Bitmap? largeCover;
    private Bitmap? preview;
    private int loadVersion;

    public LargeCoverVm(Func<string, Task<Bitmap?>> loadCover, IUiDispatcher uiDispatcher)
    {
        this.loadCover = loadCover;
        this.uiDispatcher = uiDispatcher;
    }

    public void Load(string audioPath, Bitmap? preview)
    {
        this.preview = preview;
        Release();
        _ = LoadAsync(audioPath, loadVersion);
    }

    public void Release()
    {
        loadVersion++;
        SetLargeCover(null);
    }

    private async Task LoadAsync(string audioPath, int version)
    {
        var cover = await loadCover(audioPath).ConfigureAwait(false);

        if(cover == null)
        {
            return;
        }

        uiDispatcher.Post(() =>
        {
            if(version == loadVersion)
            {
                SetLargeCover(cover);
            }
            else
            {
                cover.Dispose();
            }
        });
    }

    private void SetLargeCover(Bitmap? cover)
    {
        var previous = largeCover;
        largeCover = cover;
        Cover = cover ?? preview;
        previous?.Dispose();
    }
}
