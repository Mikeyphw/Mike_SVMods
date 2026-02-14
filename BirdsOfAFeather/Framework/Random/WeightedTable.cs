using System;
using System.Collections.Generic;

namespace Mikew.BirdsOfAFeather.Framework.Random;

/// <summary>
/// Selects items using weighted random selection.
/// </summary>
public sealed class WeightedTable<T>
{
    private readonly List<WeightedEntry<T>> _entries = new();
    private readonly IRandomProvider _random;
    private readonly bool _useInverseWeights;

    public WeightedTable(IRandomProvider random, bool useInverseWeights = false)
    {
        _random = random ?? throw new ArgumentNullException(nameof(random));
        _useInverseWeights = useInverseWeights;
    }

    public IReadOnlyList<WeightedEntry<T>> Entries => _entries;

    public void Add(T item, double weight)
    {
        _entries.Add(new WeightedEntry<T>(item, weight));
    }

    public void AddRange(IEnumerable<(T item, double weight)> entries)
    {
        if (entries is null)
        {
            throw new ArgumentNullException(nameof(entries));
        }

        foreach (var (item, weight) in entries)
        {
            Add(item, weight);
        }
    }

    public T Roll()
    {
        if (_entries.Count == 0)
        {
            throw new InvalidOperationException("Cannot roll on an empty weighted table.");
        }

        var total = 0.0;
        for (var i = 0; i < _entries.Count; i++)
        {
            total += EffectiveWeight(_entries[i].Weight);
        }

        if (total <= 0)
        {
            return _entries[0].Item;
        }

        var roll = _random.NextDouble() * total;
        for (var i = 0; i < _entries.Count; i++)
        {
            roll -= EffectiveWeight(_entries[i].Weight);
            if (roll < 0)
            {
                return _entries[i].Item;
            }
        }

        return _entries[^1].Item;
    }

    public T RollOrDefault(T fallback)
    {
        return _entries.Count == 0 ? fallback : Roll();
    }

    private double EffectiveWeight(double weight)
    {
        var w = Math.Max(0, weight);
        if (!_useInverseWeights)
        {
            return w;
        }

        return w <= 0 ? 0 : 1.0 / w;
    }
}

/// <summary>
/// A weighted entry.
/// </summary>
public readonly record struct WeightedEntry<T>(T Item, double Weight);
