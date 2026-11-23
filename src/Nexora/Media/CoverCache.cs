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
    private const int LargeMaxSide = 1024;
    private readonly ILogger<CoverCache> logger;
    private readonly ITrackCoverLoader coverLoader;
    private readonly IRenderScaleProvider renderScaleProvider;
    private readonly LruCache<CoverKey, Bitmap?> cache = new LruCache<CoverKey, Bitmap?>(Capacity);
    private readonly ConcurrentDictionary<CoverKey, Task<Bitmap?>> inFlight = [];

    public CoverCache(
        ILogger<CoverCache> logger,
        ITrackCoverLoader coverLoader,
        IRenderScaleProvider renderScaleProvider)
    {
        this.logger = logger;
        this.coverLoader = coverLoader;
        this.renderScaleProvider = renderScaleProvider;
    }

    public Bitmap? Get(string audioPath, double size)
    {
        cache.TryGet(new CoverKey(audioPath, ScaleToScreen(size)), out var bitmap);
        return bitmap;
    }

    public async Task<Bitmap?> GetOrLoadAsync(string audioPath, double size)
    {
        var key = new CoverKey(audioPath, ScaleToScreen(size));

        if(cache.TryGet(key, out var cached))
        {
            return cached;
        }

        var decodeTask = inFlight.GetOrAdd(
            key,
            it => Task.Run(() => LoadCover(it.AudioPath, stream => DecodeToFit(stream, it.PixelSide)))
        );

        try
        {
            var bitmap = await decodeTask.ConfigureAwait(false);
            cache.Set(key, bitmap);
            return bitmap;
        }
        finally
        {
            inFlight.TryRemove(key, out _);
        }
    }

    public Task<Bitmap?> LoadLargeAsync(string audioPath)
    {
        return Task.Run(() => LoadCover(audioPath, DecodeLarge));
    }

    public Task<Bitmap?> LoadOriginalAsync(string audioPath)
    {
        return Task.Run(() => LoadCover(audioPath, stream => new Bitmap(stream)));
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

    public static Bitmap DecodeToFit(Stream stream, int side)
    {
        var bitmap = Bitmap.DecodeToWidth(stream, side, BitmapInterpolationMode.HighQuality);

        if(bitmap.PixelSize.Height <= side)
        {
            return bitmap;
        }

        bitmap.Dispose();
        stream.Position = 0;
        return Bitmap.DecodeToHeight(stream, side, BitmapInterpolationMode.HighQuality);
    }

    private int ScaleToScreen(double size)
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
