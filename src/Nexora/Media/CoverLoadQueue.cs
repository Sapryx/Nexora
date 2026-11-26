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
    private readonly List<CoverRequest> delayed = [];
    private readonly List<CoverRequest> shown = [];
    private readonly List<CoverRequest> loading = [];

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

    public CoverRequest Request(string audioPath, double size, bool isPrefetch, Action<Bitmap?> loaded)
    {
        var request = new CoverRequest(audioPath, size, isPrefetch, loaded);
        delayed.RemoveAll(it => it.IsCancelled);
        delayed.Add(request);
        request.Delay(timeProvider.CreateTimer(
            _ => uiDispatcher.Post(() => OnShown(request)),
            null,
            ShowDelay,
            Timeout.InfiniteTimeSpan
        ));
        return request;
    }

    public void SetPrefetch(CoverRequest request, bool isPrefetch)
    {
        if(request.IsPrefetch == isPrefetch)
        {
            return;
        }

        request.IsPrefetch = isPrefetch;

        if(shown.Remove(request))
        {
            shown.Add(request);
        }

        StartLoads();
    }

    public void Cancel(CoverRequest request)
    {
        request.Cancel();
        StartLoads();
    }

    private void OnShown(CoverRequest request)
    {
        delayed.Remove(request);

        if(request.IsCancelled)
        {
            return;
        }

        shown.Add(request);
        StartLoads();
    }

    private void StartLoads()
    {
        shown.RemoveAll(request => request.IsCancelled);
        var request = TakeNext();

        while(request != null)
        {
            loading.Add(request);
            _ = LoadAsync(request);
            request = TakeNext();
        }
    }

    private CoverRequest? TakeNext()
    {
        if(loading.Count >= MaxConcurrentLoads)
        {
            return null;
        }

        var request = shown.LastOrDefault(it => !it.IsPrefetch);

        if(request == null && !HasPendingVisible())
        {
            request = shown.LastOrDefault();
        }

        if(request != null)
        {
            shown.Remove(request);
        }

        return request;
    }

    private bool HasPendingVisible()
    {
        return delayed.Concat(loading).Any(request => !request.IsPrefetch && !request.IsCancelled);
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
        loading.Remove(request);

        if(!request.IsCancelled)
        {
            request.Loaded(bitmap);
        }

        StartLoads();
    }
}
