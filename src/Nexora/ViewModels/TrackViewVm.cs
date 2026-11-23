using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.Input;
using Core.Playlists;
using Nexora.Media;
using Nexora.Threading;

namespace Nexora.ViewModels;

public partial class TrackViewVm : CoveredTrackVm
{
    private readonly ICoverCache coverCache;
    private readonly CoverViewerVm coverViewerVm;
    private readonly IUiDispatcher uiDispatcher;

    public TrackViewVm(
        ICoverCache coverCache,
        CoverViewerVm coverViewerVm,
        IUiDispatcher uiDispatcher) : base(coverCache)
    {
        this.coverCache = coverCache;
        this.coverViewerVm = coverViewerVm;
        this.uiDispatcher = uiDispatcher;
    }

    public void Update(IPlaylistItem playlistItem)
    {
        var metadata = playlistItem.AudioTrack.Metadata;
        SetTrackInfo(metadata.Title, metadata.JoinedArtists, playlistItem.AudioTrack.AudioPath);
    }

    [RelayCommand]
    public void ShowCover()
    {
        var cover = Cover;

        if(cover != null)
        {
            coverViewerVm.Open(AudioPath, cover);
        }
    }

    protected override Bitmap? GetCover(double size)
    {
        var cached = base.GetCover(size);

        if(cached == null)
        {
            _ = LoadCoverAsync(size);
        }

        return cached;
    }

    private async Task LoadCoverAsync(double size)
    {
        string path = AudioPath;
        var bitmap = await coverCache.GetOrLoadAsync(path, size, CancellationToken.None).ConfigureAwait(false);

        if(bitmap != null)
        {
            uiDispatcher.Post(() => ShowLoadedCover(path, size));
        }
    }
}
