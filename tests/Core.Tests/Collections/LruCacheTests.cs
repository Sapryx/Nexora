using Core.Collections;

namespace Core.Tests.Collections;

public class LruCacheTests
{
    [Fact]
    public void TryGet_EmptyCache_ReturnsFalse()
    {
        var cache = new LruCache<string, int>(2, _ => 1);

        Assert.False(cache.TryGet("a", out _));
    }

    [Fact]
    public void Set_ThenTryGet_ReturnsValue()
    {
        var cache = new LruCache<string, int>(2, _ => 1);

        cache.Set("a", 1);

        Assert.True(cache.TryGet("a", out int value));
        Assert.Equal(1, value);
    }

    [Fact]
    public void Set_ExistingKey_ReplacesValue()
    {
        var cache = new LruCache<string, int>(2, _ => 1);

        cache.Set("a", 1);
        cache.Set("a", 2);

        Assert.True(cache.TryGet("a", out int value));
        Assert.Equal(2, value);
        Assert.Equal(1, cache.Count);
    }

    [Fact]
    public void Set_BeyondCapacity_EvictsLeastRecentlyUsed()
    {
        var cache = new LruCache<string, int>(2, _ => 1);

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
        var cache = new LruCache<string, int>(2, _ => 1);

        cache.Set("a", 1);
        cache.Set("b", 2);
        cache.TryGet("a", out _); // "a" is now more recent than "b"
        cache.Set("c", 3);

        Assert.True(cache.TryGet("a", out _));
        Assert.False(cache.TryGet("b", out _));
    }

    [Fact]
    public void Constructor_NegativeCapacity_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new LruCache<string, int>(-1, _ => 1));
    }

    [Fact]
    public void Capacity_SetNegative_Throws()
    {
        var cache = new LruCache<string, int>(2, _ => 1);

        Assert.Throws<ArgumentOutOfRangeException>(() => cache.Capacity = -1);
    }

    [Fact]
    public void Set_WeightedValues_EvictsOldestUntilWeightFitsCapacity()
    {
        var cache = new LruCache<string, int>(10, value => value);

        cache.Set("a", 4);
        cache.Set("b", 3);
        cache.Set("c", 8);

        Assert.Equal(8, cache.Weight);
        Assert.False(cache.TryGet("a", out _));
        Assert.False(cache.TryGet("b", out _));
        Assert.True(cache.TryGet("c", out _));
    }

    [Fact]
    public void Set_ExistingKey_ReplacesItsWeight()
    {
        var cache = new LruCache<string, int>(10, value => value);

        cache.Set("a", 4);
        cache.Set("a", 7);

        Assert.Equal(7, cache.Weight);
    }

    [Fact]
    public void Set_ValueHeavierThanCapacity_IsNotKept()
    {
        var cache = new LruCache<string, int>(5, value => value);

        cache.Set("a", 6);

        Assert.Equal(0, cache.Count);
        Assert.Equal(0, cache.Weight);
    }

    [Fact]
    public void Set_ZeroWeightValues_AreKeptAtZeroCapacity()
    {
        var cache = new LruCache<string, int>(0, value => value);

        cache.Set("a", 0);

        Assert.True(cache.TryGet("a", out _));
    }

    [Fact]
    public void Capacity_Lowered_EvictsLeastRecentlyUsed()
    {
        var cache = new LruCache<string, int>(10, value => value);
        cache.Set("a", 4);
        cache.Set("b", 3);

        cache.Capacity = 5;

        Assert.Equal(5, cache.Capacity);
        Assert.Equal(3, cache.Weight);
        Assert.False(cache.TryGet("a", out _));
        Assert.True(cache.TryGet("b", out _));
    }

    [Fact]
    public void Capacity_Raised_KeepsEntriesAndAcceptsMore()
    {
        var cache = new LruCache<string, int>(4, value => value);
        cache.Set("a", 4);

        cache.Capacity = 8;
        cache.Set("b", 4);

        Assert.True(cache.TryGet("a", out _));
        Assert.True(cache.TryGet("b", out _));
    }

    [Fact]
    public async Task SetAndTryGet_ConcurrentAccess_CountStaysWithinCapacity()
    {
        var cache = new LruCache<int, int>(64, _ => 1);
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
