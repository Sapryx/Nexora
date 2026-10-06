using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Core.Collections;
using Core.Storage;

namespace Nexora.Media;

public class CoverCache : ICoverCache
{
    private const int Capacity = 256;
    private const int DecodeWidth = 128;
    
    private readonly ITrackCoverLoader coverLoader;
    private readonly LruCache<string, Bitmap?> cache = new(Capacity);
    private readonly ConcurrentDictionary<string, Task<Bitmap?>> inFlight = [];

    public CoverCache(ITrackCoverLoader coverLoader)
    {
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

        var decodeTask = inFlight.GetOrAdd(audioPath, path => Task.Run(() => Decode(coverLoader.LoadCover(path))));

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
