using System;
using System.Collections.Generic;
using System.Linq;

namespace WildEggsHatching.Core.Weighting;

/// <summary>
/// Deterministic weighted picker (seeded) to avoid rerolls on reload.
/// </summary>
public static class DeterministicWeightedPicker
{
    public static bool TryPickIndex(IReadOnlyList<double> weights, int seed, out int index)
    {
        index = -1;

        if (weights is null || weights.Count == 0)
            return false;

        var finite = weights.Select(w => double.IsFinite(w) && w > 0 ? w : 0).ToArray();
        var total = finite.Sum();

        if (total <= 0)
        {
            index = 0;
            return true;
        }

        var rng = new Random(seed);
        var roll = rng.NextDouble() * total;

        for (var i = 0; i < finite.Length; i++)
        {
            roll -= finite[i];
            if (roll < 0)
            {
                index = i;
                return true;
            }
        }

        index = finite.Length - 1;
        return true;
    }

    public static bool TryPick<T>(IReadOnlyList<T> orderedItems, IReadOnlyList<double> weights, int seed, out T picked)
    {
        picked = default!;

        if (orderedItems is null || weights is null)
            return false;

        if (orderedItems.Count == 0 || orderedItems.Count != weights.Count)
            return false;

        if (!TryPickIndex(weights, seed, out var idx))
            return false;

        picked = orderedItems[idx];
        return true;
    }

    public static bool TryPickKey<TKey>(
        IReadOnlyDictionary<TKey, double> weights,
        int seed,
        out TKey picked,
        IComparer<TKey>? comparer = null)
        where TKey : notnull
    {
        picked = default!;

        if (weights is null || weights.Count == 0)
            return false;

        var ordered = comparer is null
            ? weights.Keys.OrderBy(k => k).ToList()
            : weights.Keys.OrderBy(k => k, comparer).ToList();

        var ws = ordered.Select(k => weights.TryGetValue(k, out var w) ? w : 0).ToList();

        return TryPick(ordered, ws, seed, out picked);
    }
}
