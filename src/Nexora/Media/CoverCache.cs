using System.Collections.Concurrent;
using Avalonia;
using Avalonia.Media.Imaging;
using Core.Collections;
using Core.Logging;
using Core.Storage.Tags;
using Microsoft.Extensions.Logging;
using Nexora.Render;

namespace Nexora.Media;

public class CoverCache : ICoverCache
{
    private const int Capacity = 256;
    private const int PreviewWidth = 256;
    private const int LargeMaxSide = 1024;
    private readonly ILogger<CoverCache> logger;
    private readonly ITrackCoverLoader coverLoader;
    private readonly RenderScaleProvider renderScaleProvider;
    private readonly LruCache<string, Bitmap?> cache = new LruCache<string, Bitmap?>(Capacity);
    private readonly ConcurrentDictionary<string, Task<Bitmap?>> inFlight = [];

    public CoverCache(
        ILogger<CoverCache> logger,
        ITrackCoverLoader coverLoader,
        RenderScaleProvider renderScaleProvider
    )
    {
        this.logger = logger;
        this.coverLoader = coverLoader;
        this.renderScaleProvider = renderScaleProvider;
    }

    public Bitmap? Get(string audioPath)
    {
        cache.TryGet(audioPath, out var bitmap);
        return bitmap;
    }

    public async Task<Bitmap?> GetOrLoadAsync(string audioPath)
    {
        if(cache.TryGet(audioPath, out var cached))
        {
            return cached;
        }

        var decodeTask = inFlight.GetOrAdd(audioPath, path => Task.Run(() => LoadCover(path, DecodePreview)));

        try
        {
            var bitmap = await decodeTask.ConfigureAwait(false);
            cache.Set(audioPath, bitmap);
            return bitmap;
        }
        finally
        {
            inFlight.TryRemove(audioPath, out _);
        }
    }

    public Task<Bitmap?> LoadLargeAsync(string audioPath)
    {
        return Task.Run(() => LoadCover(audioPath, DecodeLarge));
    }

    public static PixelSize FitToMaxSide(PixelSize size, int maxSide)
    {
        int largestSide = Math.Max(size.Width, size.Height);

        if(largestSide <= maxSide)
        {
            return size;
        }

        double scale = (double)maxSide / largestSide;

        return new PixelSize(
            Math.Max(1, (int)Math.Round(size.Width * scale)),
            Math.Max(1, (int)Math.Round(size.Height * scale))
        );
    }

    private int ScaleToScreen(int size)
    {
        return (int)Math.Ceiling(size * renderScaleProvider.RenderScaling);
    }

    private Bitmap? LoadCover(string audioPath, Func<Stream, Bitmap> decode)
    {
        try
        {
            var coverRaw = coverLoader.LoadCover(audioPath);

            if(coverRaw == null)
            {
                return null;
            }

            using var stream = new MemoryStream(coverRaw);
            return decode(stream);
        }
        catch(Exception ex)
        {
            logger.Warn(ex, $"Failed to load cover of {audioPath}");
            return null;
        }
    }

    private Bitmap DecodePreview(Stream stream)
    {
        return Bitmap.DecodeToWidth(stream, ScaleToScreen(PreviewWidth), BitmapInterpolationMode.HighQuality);
    }

    private Bitmap DecodeLarge(Stream stream)
    {
        var bitmap = new Bitmap(stream);
        var size = FitToMaxSide(bitmap.PixelSize, ScaleToScreen(LargeMaxSide));

        if(size == bitmap.PixelSize)
        {
            return bitmap;
        }

        using(bitmap)
        {
            return bitmap.CreateScaledBitmap(size, BitmapInterpolationMode.HighQuality);
        }
    }
}
