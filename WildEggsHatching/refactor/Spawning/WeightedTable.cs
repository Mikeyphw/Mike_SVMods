using System;
using System.Collections.Generic;
using System.Linq;
using WildEggsHatching.Core.Random;

namespace WildEggsHatching.Spawning;

/// <summary>
/// Represents an item with an associated weight for weighted random selection.
/// </summary>
/// <typeparam name="T">The type of item being weighted.</typeparam>
public sealed class WeightedEntry<T>
{
    /// <summary>The item itself.</summary>
    public T Item { get; }

    /// <summary>The weight for selection (higher = more likely).</summary>
    public int Weight { get; }

    public WeightedEntry(T item, int weight)
    {
        Item = item;
        Weight = Math.Max(0, weight);
    }
}

/// <summary>
/// Selects items from a weighted table using weighted random selection.
/// </summary>
/// <typeparam name="T">The type of item to select.</typeparam>
public sealed class WeightedTable<T>
{
    /// <summary>The weighted entries in the table.</summary>
    private readonly List<WeightedEntry<T>> _entries;

    /// <summary>The random provider for RNG operations.</summary>
    private readonly IRandomProvider? _randomProvider;

    /// <summary>
    /// Creates a new weighted table from entries.
    /// </summary>
    /// <param name="entries">The weighted entries to select from.</param>
    /// <param name="randomProvider">Optional random provider for seeded RNG.</param>
    public WeightedTable(IEnumerable<WeightedEntry<T>> entries, IRandomProvider? randomProvider = null)
    {
        _entries = entries?.ToList() ?? new List<WeightedEntry<T>>();
        _randomProvider = randomProvider;
    }

    /// <summary>
    /// Creates a new weighted table from a dictionary of item to weight.
    /// </summary>
    /// <param name="weights">Dictionary mapping items to weights.</param>
    /// <param name="randomProvider">Optional random provider for seeded RNG.</param>
    public WeightedTable(Dictionary<T, int> weights, IRandomProvider? randomProvider = null)
    {
        _entries = weights
            .Select(kvp => new WeightedEntry<T>(kvp.Key, kvp.Value))
            .ToList();
        _randomProvider = randomProvider;
    }

    /// <summary>
    /// Randomly selects an item based on weights using the configured RNG.
    /// </summary>
    /// <returns>A randomly selected item.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the table is empty.</exception>
    public T Roll()
    {
        if (_entries.Count == 0)
        {
            throw new InvalidOperationException("Cannot roll on an empty weighted table.");
        }

        int totalWeight = _entries.Sum(e => e.Weight);

        if (totalWeight <= 0)
        {
            // If no positive weights, return first item
            return _entries[0].Item;
        }

        int roll;
        if (_randomProvider != null)
        {
            roll = _randomProvider.Next(totalWeight);
        }
        else
        {
            // Fallback: use a new Random instance (should not happen in production)
            roll = new Random().Next(totalWeight);
        }

        int cumulative = 0;

        foreach (var entry in _entries)
        {
            cumulative += entry.Weight;
            if (roll < cumulative)
            {
                return entry.Item;
            }
        }

        // Should not reach here, but return last item as fallback
        return _entries[^1].Item;
    }

    /// <summary>
    /// Randomly selects an item based on weights using a specific random source.
    /// </summary>
    /// <param name="random">The random to use (uses Game1.random if null).</param>
    /// <returns>A randomly selected item.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the table is empty.</exception>
    public T Roll(Random? random)
    {
        if (_entries.Count == 0)
        {
            throw new InvalidOperationException("Cannot roll on an empty weighted table.");
        }

        int totalWeight = _entries.Sum(e => e.Weight);

        if (totalWeight <= 0)
        {
            return _entries[0].Item;
        }

        int roll = random?.Next(totalWeight) ?? new Random().Next(totalWeight);
        int cumulative = 0;

        foreach (var entry in _entries)
        {
            cumulative += entry.Weight;
            if (roll < cumulative)
            {
                return entry.Item;
            }
        }

        return _entries[^1].Item;
    }

    /// <summary>
    /// Gets all entries in the table.
    /// </summary>
    public IReadOnlyList<WeightedEntry<T>> Entries => _entries.AsReadOnly();

    /// <summary>
    /// Gets the total weight of all entries.
    /// </summary>
    public int TotalWeight => _entries.Sum(e => e.Weight);
}


