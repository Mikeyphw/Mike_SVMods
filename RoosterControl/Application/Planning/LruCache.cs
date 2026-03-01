using System;
using System.Collections.Generic;

namespace RoosterControl.Application.Planning;

/// <summary>
/// Fixed-capacity least-recently-used cache.
/// </summary>
/// <remarks>
/// This is intentionally small and dependency-free so it can be used by RoosterControl.Pure.
/// </remarks>
internal sealed class LruCache<TKey, TValue>
    where TKey : notnull
{
    private readonly int _capacity;
    private readonly Dictionary<TKey, LinkedListNode<Entry>> _map;
    private readonly LinkedList<Entry> _lru = new();

    public int Capacity => _capacity;
    public int Count => _map.Count;

    public long Evictions { get; private set; }

    public LruCache(int capacity, IEqualityComparer<TKey>? comparer = null)
    {
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "capacity must be > 0");

        _capacity = capacity;
        _map = new Dictionary<TKey, LinkedListNode<Entry>>(comparer ?? EqualityComparer<TKey>.Default);
    }

    public bool TryGetValue(TKey key, out TValue value)
    {
        if (_map.TryGetValue(key, out var node))
        {
            _lru.Remove(node);
            _lru.AddFirst(node);
            value = node.Value.Value;
            return true;
        }

        value = default!;
        return false;
    }

    public void Set(TKey key, TValue value)
    {
        if (_map.TryGetValue(key, out var existing))
        {
            existing.Value = new Entry(key, value);
            _lru.Remove(existing);
            _lru.AddFirst(existing);
            return;
        }

        if (_map.Count >= _capacity)
            EvictOne();

        var node = new LinkedListNode<Entry>(new Entry(key, value));
        _lru.AddFirst(node);
        _map[key] = node;
    }

    public void Clear(bool resetStats = true)
    {
        _map.Clear();
        _lru.Clear();

        if (resetStats)
            Evictions = 0;
    }

    private void EvictOne()
    {
        var node = _lru.Last;
        if (node is null)
            return;

        _lru.RemoveLast();
        _map.Remove(node.Value.Key);
        Evictions++;
    }

    private readonly record struct Entry(TKey Key, TValue Value);
}
