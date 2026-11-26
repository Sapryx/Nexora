using System.Diagnostics.CodeAnalysis;

namespace Core.Collections;

public class LruCache<TKey, TValue> where TKey : notnull
{
    public int Count
    {
        get
        {
            lock(gate)
            {
                return map.Count;
            }
        }
    }

    public long Weight
    {
        get
        {
            lock(gate)
            {
                return weight;
            }
        }
    }

    public long Capacity
    {
        get
        {
            lock(gate)
            {
                return capacity;
            }
        }
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);

            lock(gate)
            {
                capacity = value;
                EvictOverCapacity();
            }
        }
    }

    private readonly Lock gate = new Lock();
    private readonly Func<TValue, long> weigh;
    private readonly Dictionary<TKey, LinkedListNode<CacheEntry>> map = [];
    private readonly LinkedList<CacheEntry> order = [];
    private long capacity;
    private long weight;

    public LruCache(long capacity, Func<TValue, long> weigh)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);
        this.capacity = capacity;
        this.weigh = weigh;
    }

    public bool TryGet(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        lock(gate)
        {
            if(map.TryGetValue(key, out var node))
            {
                order.Remove(node);
                order.AddFirst(node);
                value = node.Value.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    public void Set(TKey key, TValue value)
    {
        var entry = new CacheEntry(key, value, weigh(value));

        lock(gate)
        {
            if(map.Remove(key, out var existing))
            {
                order.Remove(existing);
                weight -= existing.Value.Weight;
            }

            map[key] = order.AddFirst(entry);
            weight += entry.Weight;
            EvictOverCapacity();
        }
    }

    private void EvictOverCapacity()
    {
        while(weight > capacity)
        {
            var oldest = order.Last!.Value;
            order.RemoveLast();
            map.Remove(oldest.Key);
            weight -= oldest.Weight;
        }
    }

    private readonly record struct CacheEntry(TKey Key, TValue Value, long Weight);
}
