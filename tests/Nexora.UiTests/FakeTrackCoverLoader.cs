using System.Collections.Concurrent;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Core.Storage.Tags;

namespace Nexora.UiTests;

public sealed class FakeTrackCoverLoader : ITrackCoverLoader
{
    private readonly ConcurrentDictionary<string, byte[]> covers = [];

    public void Add(string audioPath, PixelSize size)
    {
        covers[audioPath] = CreatePng(size);
    }

    public byte[]? LoadCover(string audioPath)
    {
        return covers.GetValueOrDefault(audioPath);
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
