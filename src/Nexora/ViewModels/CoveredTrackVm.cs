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
    public Bitmap? Cover => GetCover(CoverSizes.Thumbnail, nameof(Cover));
    public Bitmap? TileCover => GetCover(CoverSizes.Tile, nameof(TileCover));
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
            OnPropertyChanged(nameof(TileCover));
        }
    }

    private Bitmap? GetCover(double size, string propertyName)
    {
        var cached = coverCache.Get(AudioPath, size);

        if(cached == null)
        {
            _ = LoadCoverAsync(size, propertyName);
        }

        return cached;
    }

    private async Task LoadCoverAsync(double size, string propertyName)
    {
        string path = AudioPath;
        var bitmap = await coverCache.GetOrLoadAsync(path, size).ConfigureAwait(false);

        if(bitmap != null)
        {
            uiDispatcher.Post(() =>
            {
                if(path == AudioPath)
                {
                    OnPropertyChanged(propertyName);
                }
            });
        }
    }
}
