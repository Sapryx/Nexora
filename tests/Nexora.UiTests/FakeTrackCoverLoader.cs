using System.Collections.Concurrent;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Core.Storage.Tags;

namespace Nexora.UiTests;

public sealed class FakeTrackCoverLoader : ITrackCoverLoader
{
    private readonly ConcurrentDictionary<string, byte[]> covers = [];
    private readonly ConcurrentDictionary<string, int> loadCounts = [];
    private byte[]? defaultCover;

    public void Add(string audioPath, PixelSize size)
    {
        covers[audioPath] = CreatePng(size);
    }

    public void AddToEveryTrack(PixelSize size)
    {
        defaultCover = CreatePng(size);
    }

    public int LoadCount(string audioPath)
    {
        return loadCounts.GetValueOrDefault(audioPath);
    }

    public byte[]? LoadCover(string audioPath)
    {
        loadCounts.AddOrUpdate(audioPath, 1, (_, count) => count + 1);
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
