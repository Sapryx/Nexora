using Avalonia.Media.Imaging;

namespace Nexora.Media;

public sealed class CoverRequest
{
    public string AudioPath { get; }
    public double Size { get; }
    public Action<Bitmap?> Loaded { get; }
    public bool IsPrefetch { get; set; }
    public CancellationToken Token => cancellation.Token;
    public bool IsCancelled => cancellation.IsCancellationRequested;
    private readonly CancellationTokenSource cancellation = new CancellationTokenSource();
    private ITimer? delayTimer;

    public CoverRequest(string audioPath, double size, bool isPrefetch, Action<Bitmap?> loaded)
    {
        AudioPath = audioPath;
        Size = size;
        IsPrefetch = isPrefetch;
        Loaded = loaded;
    }

    public void Delay(ITimer timer)
    {
        delayTimer = timer;
    }

    public void Cancel()
    {
        delayTimer?.Dispose();
        cancellation.Cancel();
    }
}
