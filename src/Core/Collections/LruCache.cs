namespace Core.Collections;

public class LruCache<TKey, TValue> where TKey : notnull
{
    private readonly Lock gate = new Lock();
    private readonly int capacity;
    private readonly Dictionary<TKey, LinkedListNode<CacheEntry>> map = [];
    private readonly LinkedList<CacheEntry> order = [];

    public LruCache(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1);
        this.capacity = capacity;
    }

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

    public bool TryGet(TKey key, out TValue value)
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

        value = default!;
        return false;
    }

    public void Set(TKey key, TValue value)
    {
        lock(gate)
        {
            if(map.TryGetValue(key, out var existing))
            {
                existing.Value = new CacheEntry(key, value);
                order.Remove(existing);
                order.AddFirst(existing);
                return;
            }

            var node = new LinkedListNode<CacheEntry>(new CacheEntry(key, value));
            map[key] = node;
            order.AddFirst(node);

            if(map.Count > capacity)
            {
                var oldest = order.Last!;
                order.RemoveLast();
                map.Remove(oldest.Value.Key);
            }
        }
    }

    private readonly record struct CacheEntry(TKey Key, TValue Value);
}
