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
    public long MemoryBudget
    {
        get => cache.Capacity;
        set => cache.Capacity = value;
    }

    public long MemoryUsed => cache.Weight;
    private const int LargeMaxSide = 1024;
    private readonly ILogger<CoverCache> logger;
    private readonly ITrackCoverLoader coverLoader;
    private readonly IThumbnailStore thumbnailStore;
    private readonly IRenderScaleProvider renderScaleProvider;
    private readonly LruCache<CoverKey, Bitmap?> cache = new LruCache<CoverKey, Bitmap?>(0, WeighCover);

    public CoverCache(
        ILogger<CoverCache> logger,
        ITrackCoverLoader coverLoader,
        IThumbnailStore thumbnailStore,
        IRenderScaleProvider renderScaleProvider)
    {
        this.logger = logger;
        this.coverLoader = coverLoader;
        this.thumbnailStore = thumbnailStore;
        this.renderScaleProvider = renderScaleProvider;
    }

    public Bitmap? Get(string audioPath, double size)
    {
        cache.TryGet(new CoverKey(audioPath, ScaleToScreen(size)), out var bitmap);
        return bitmap;
    }

    public async Task<Bitmap?> GetOrLoadAsync(string audioPath, double size, CancellationToken token)
    {
        var key = new CoverKey(audioPath, ScaleToScreen(size));

        if(cache.TryGet(key, out var cached))
        {
            return cached;
        }

        var bitmap = await Task.Run(() => LoadThumbnail(key, token), token).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();
        cache.Set(key, bitmap);
        return bitmap;
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

    private static long WeighCover(Bitmap? bitmap)
    {
        return bitmap == null ? 0 : (long)bitmap.PixelSize.Width * bitmap.PixelSize.Height * CoverSizes.BytesPerPixel;
    }

    private int ScaleToScreen(double size)
    {
        return CoverSizes.ToPixels(size, renderScaleProvider.RenderScaling);
    }

    private Bitmap? LoadThumbnail(CoverKey key, CancellationToken token)
    {
        if(thumbnailStore.TryLoad(key.AudioPath, key.PixelSide, out var stored))
        {
            return stored;
        }

        if(TryReadCover(key.AudioPath, stream => DecodeToFit(stream, key.PixelSide), token, out var bitmap))
        {
            thumbnailStore.Save(key.AudioPath, key.PixelSide, bitmap);
        }

        return bitmap;
    }

    private Bitmap? LoadCover(string audioPath, Func<Stream, Bitmap> decode)
    {
        TryReadCover(audioPath, decode, CancellationToken.None, out var bitmap);
        return bitmap;
    }

    private bool TryReadCover(
        string audioPath,
        Func<Stream, Bitmap> decode,
        CancellationToken token,
        out Bitmap? bitmap)
    {
        bitmap = null;

        try
        {
            var coverRaw = coverLoader.LoadCover(audioPath);
            token.ThrowIfCancellationRequested();

            if(coverRaw != null)
            {
                using var stream = new MemoryStream(coverRaw);
                bitmap = decode(stream);
            }

            return true;
        }
        catch(Exception ex) when(ex is not OperationCanceledException)
        {
            logger.Warn(ex, $"Failed to load cover of {audioPath}");
            return false;
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
