using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Nexora.Media;
using Nexora.Threading;

namespace Nexora.ViewModels;

public abstract partial class CoveredTrackVm : ViewModelBase
{
    [ObservableProperty]
    public partial string Title { get; set; } = "";

    [ObservableProperty]
    public partial string Artists { get; set; } = "";

    public string AudioPath { get; private set; } = "";

    public Bitmap? Cover
    {
        get
        {
            var cached = coverCache.Get(AudioPath);

            if(cached == null)
            {
                _ = LoadCoverAsync();
            }

            return cached;
        }
    }

    private readonly ICoverCache coverCache;
    private readonly IUiDispatcher uiDispatcher;

    protected CoveredTrackVm(ICoverCache coverCache, IUiDispatcher uiDispatcher)
    {
        this.coverCache = coverCache;
        this.uiDispatcher = uiDispatcher;
    }

    protected void SetTrackInfo(string title, string artists, string audioPath)
    {
        Title = title;
        Artists = artists;
        bool pathChanged = AudioPath != audioPath;
        AudioPath = audioPath;

        if(pathChanged)
        {
            OnPropertyChanged(nameof(Cover));
        }
    }

    private async Task LoadCoverAsync()
    {
        string path = AudioPath;
        var bitmap = await coverCache.GetOrLoadAsync(path).ConfigureAwait(false);

        if(bitmap != null)
        {
            uiDispatcher.Post(() =>
            {
                if(path == AudioPath)
                {
                    OnPropertyChanged(nameof(Cover));
                }
            });
        }
    }
}
