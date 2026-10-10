using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Core.Collections;
using Core.Logging;
using Core.Storage;
using Microsoft.Extensions.Logging;

namespace Nexora.Media;

public class CoverCache : ICoverCache
{
    private const int Capacity = 256;
    private const int DecodeWidth = 128;
    private readonly ILogger<CoverCache> logger;
    private readonly ITrackCoverLoader coverLoader;
    private readonly LruCache<string, Bitmap?> cache = new LruCache<string, Bitmap?>(Capacity);
    private readonly ConcurrentDictionary<string, Task<Bitmap?>> inFlight = [];

    public CoverCache(ILogger<CoverCache> logger, ITrackCoverLoader coverLoader)
    {
        this.logger = logger;
        this.coverLoader = coverLoader;
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

        var decodeTask = inFlight.GetOrAdd(audioPath, path => Task.Run(() => LoadCover(path)));

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

    private Bitmap? LoadCover(string audioPath)
    {
        try
        {
            return Decode(coverLoader.LoadCover(audioPath));
        }
        catch(Exception ex)
        {
            logger.Warn(ex, $"Failed to load cover of {audioPath}");
            return null;
        }
    }

    private static Bitmap? Decode(byte[]? coverRaw)
    {
        if(coverRaw == null)
        {
            return null;
        }

        using var stream = new MemoryStream(coverRaw);
        return Bitmap.DecodeToWidth(stream, DecodeWidth, BitmapInterpolationMode.HighQuality);
    }
}
