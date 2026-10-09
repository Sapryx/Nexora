using Core.Collections;

namespace Core.Tests.Collections;

public class LruCacheTests
{
    [Fact]
    public void TryGet_EmptyCache_ReturnsFalse()
    {
        var cache = new LruCache<string, int>(2);

        Assert.False(cache.TryGet("a", out _));
    }

    [Fact]
    public void Set_ThenTryGet_ReturnsValue()
    {
        var cache = new LruCache<string, int>(2);

        cache.Set("a", 1);

        Assert.True(cache.TryGet("a", out var value));
        Assert.Equal(1, value);
    }

    [Fact]
    public void Set_ExistingKey_ReplacesValue()
    {
        var cache = new LruCache<string, int>(2);

        cache.Set("a", 1);
        cache.Set("a", 2);

        Assert.True(cache.TryGet("a", out var value));
        Assert.Equal(2, value);
        Assert.Equal(1, cache.Count);
    }

    [Fact]
    public void Set_BeyondCapacity_EvictsLeastRecentlyUsed()
    {
        var cache = new LruCache<string, int>(2);

        cache.Set("a", 1);
        cache.Set("b", 2);
        cache.Set("c", 3);

        Assert.Equal(2, cache.Count);
        Assert.False(cache.TryGet("a", out _));
        Assert.True(cache.TryGet("b", out _));
        Assert.True(cache.TryGet("c", out _));
    }

    [Fact]
    public void TryGet_RefreshesRecency_KeepingEntryAlive()
    {
        var cache = new LruCache<string, int>(2);

        cache.Set("a", 1);
        cache.Set("b", 2);
        cache.TryGet("a", out _); // "a" is now more recent than "b"
        cache.Set("c", 3);

        Assert.True(cache.TryGet("a", out _));
        Assert.False(cache.TryGet("b", out _));
    }

    [Fact]
    public void Constructor_ZeroCapacity_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new LruCache<string, int>(0));
    }

    [Fact]
    public async Task SetAndTryGet_ConcurrentAccess_CountStaysWithinCapacity()
    {
        var cache = new LruCache<int, int>(64);
        var tasks = Enumerable.Range(0, 8).Select(worker => Task.Run(() =>
        {
            for(int i = 0; i < 10_000; i++)
            {
                int key = (worker * 1000 + i) % 128;
                cache.Set(key, key);
                cache.TryGet(key, out _);
            }
        })).ToArray();

        await Task.WhenAll(tasks);

        Assert.True(cache.Count <= 64);
    }
}
