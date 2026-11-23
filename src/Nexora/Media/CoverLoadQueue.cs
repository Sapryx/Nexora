using Avalonia.Media.Imaging;
using Nexora.Threading;

namespace Nexora.Media;

public class CoverLoadQueue
{
    public TimeSpan ShowDelay { get; }
    public int MaxConcurrentLoads { get; }
    private readonly ICoverCache coverCache;
    private readonly TimeProvider timeProvider;
    private readonly IUiDispatcher uiDispatcher;
    private readonly List<CoverRequest> shown = [];
    private int activeLoads;

    public CoverLoadQueue(
        ICoverCache coverCache,
        TimeProvider timeProvider,
        IUiDispatcher uiDispatcher,
        TimeSpan showDelay,
        int maxConcurrentLoads)
    {
        this.coverCache = coverCache;
        this.timeProvider = timeProvider;
        this.uiDispatcher = uiDispatcher;
        ShowDelay = showDelay;
        MaxConcurrentLoads = maxConcurrentLoads;
    }

    public CoverRequest Request(string audioPath, double size, Action<Bitmap?> loaded)
    {
        var request = new CoverRequest(audioPath, size, loaded);
        request.Delay(timeProvider.CreateTimer(
            _ => uiDispatcher.Post(() => OnShown(request)),
            null,
            ShowDelay,
            Timeout.InfiniteTimeSpan
        ));
        return request;
    }

    private void OnShown(CoverRequest request)
    {
        if(request.IsCancelled)
        {
            return;
        }

        shown.Add(request);
        StartLoads();
    }

    private void StartLoads()
    {
        while(activeLoads < MaxConcurrentLoads && shown.Count > 0)
        {
            var request = shown[shown.Count - 1];
            shown.RemoveAt(shown.Count - 1);

            if(!request.IsCancelled)
            {
                activeLoads++;
                _ = LoadAsync(request);
            }
        }
    }

    private async Task LoadAsync(CoverRequest request)
    {
        try
        {
            var bitmap = await coverCache.GetOrLoadAsync(request.AudioPath, request.Size, request.Token)
                .ConfigureAwait(false);
            uiDispatcher.Post(() => OnLoadEnded(request, bitmap));
        }
        catch(OperationCanceledException)
        {
            uiDispatcher.Post(() => OnLoadEnded(request, null));
        }
    }

    private void OnLoadEnded(CoverRequest request, Bitmap? bitmap)
    {
        activeLoads--;

        if(!request.IsCancelled)
        {
            request.Loaded(bitmap);
        }

        StartLoads();
    }
}
