using System.Collections.Concurrent;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Core.Storage.Tags;

namespace Nexora.UiTests;

public sealed class FakeTrackCoverLoader : ITrackCoverLoader
{
    public IReadOnlyList<string> LoadOrder => loadOrder.ToList();
    private readonly ConcurrentDictionary<string, byte[]> covers = [];
    private readonly ConcurrentDictionary<string, int> loadCounts = [];
    private readonly ConcurrentQueue<string> loadOrder = [];
    private const int MaxHeldLoads = 1000;
    private volatile SemaphoreSlim? heldLoads;
    private byte[]? defaultCover;

    public void Add(string audioPath, PixelSize size)
    {
        covers[audioPath] = CreatePng(size);
    }

    public void AddToEveryTrack(PixelSize size)
    {
        defaultCover = CreatePng(size);
    }

    public void Hold()
    {
        heldLoads = new SemaphoreSlim(0);
    }

    public void ReleaseOne()
    {
        heldLoads?.Release();
    }

    public void Release()
    {
        var released = heldLoads;
        heldLoads = null;
        released?.Release(MaxHeldLoads);
    }

    public int LoadCount(string audioPath)
    {
        return loadCounts.GetValueOrDefault(audioPath);
    }

    public byte[]? LoadCover(string audioPath)
    {
        loadCounts.AddOrUpdate(audioPath, 1, (_, count) => count + 1);
        loadOrder.Enqueue(audioPath);

        heldLoads?.Wait();
        return covers.GetValueOrDefault(audioPath) ?? defaultCover;
    }

    public static byte[] CreatePng(PixelSize size)
    {
        using var bitmap = new RenderTargetBitmap(size);

        using(var context = bitmap.CreateDrawingContext())
        {
            context.FillRectangle(Brushes.SteelBlue, new Rect(0, 0, size.Width, size.Height));
            context.FillRectangle(Brushes.Orange, new Rect(0, 0, size.Width / 2.0, size.Height / 2.0));
        }

        using var stream = new MemoryStream();
        bitmap.Save(stream, PngBitmapEncoderOptions.Default);
        return stream.ToArray();
    }
}
