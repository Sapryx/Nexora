using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Nexora.Media;

namespace Nexora.ViewModels;

public abstract partial class CoveredTrackVm : ViewModelBase
{
    [ObservableProperty]
    public partial string Title { get; set; } = "";

    [ObservableProperty]
    public partial string Artists { get; set; } = "";

    public string AudioPath { get; private set; } = "";
    public Bitmap? Cover => GetCover(CoverSizes.Thumbnail);
    public Bitmap? TileCover => GetCover(CoverSizes.Tile);
    private readonly ICoverCache coverCache;

    protected CoveredTrackVm(ICoverCache coverCache)
    {
        this.coverCache = coverCache;
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

    protected virtual Bitmap? GetCover(double size)
    {
        return coverCache.Get(AudioPath, size);
    }

    protected void ShowLoadedCover(string audioPath, double size)
    {
        if(audioPath == AudioPath)
        {
            OnPropertyChanged(size == CoverSizes.Tile ? nameof(TileCover) : nameof(Cover));
        }
    }
}
