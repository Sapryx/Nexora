using System.Threading.Tasks;
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

    private string audioPath = "";
    private ICoverCache CoverCache { get; }

    public Bitmap? Cover
    {
        get
        {
            var cached = CoverCache.Get(audioPath);

            if(cached == null)
            {
                _ = LoadCoverAsync();
            }

            return cached;
        }
    }

    protected CoveredTrackVm(ICoverCache coverCache)
    {
        CoverCache = coverCache;
    }

    protected void SetTrackInfo(string title, string artists, string audioPath)
    {
        Title = title;
        Artists = artists;
        bool pathChanged = this.audioPath != audioPath;
        this.audioPath = audioPath;

        if(pathChanged)
        {
            OnPropertyChanged(nameof(Cover));
        }
    }

    private async Task LoadCoverAsync()
    {
        var path = audioPath;
        var bitmap = await CoverCache.GetOrLoadAsync(path).ConfigureAwait(false);

        if(path == audioPath && bitmap != null)
        {
            OnPropertyChanged(nameof(Cover));
        }
    }
}
