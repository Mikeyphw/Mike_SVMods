using System;
using System.Collections.Generic;
using System.Linq;
using Mikew.NestCore.Framework.Random;

namespace LetMeHatch.Core.Breeding;

/// <summary>
/// Pure egg-selection helpers (chance rolls + weighted choice) built on NestCore RNG abstractions.
/// </summary>
public sealed class EggSelectionService
{
    private readonly IRandomProvider _rng;

    public EggSelectionService(IRandomProvider rng)
    {
        _rng = rng ?? throw new ArgumentNullException(nameof(rng));
    }

    public bool RollChance(double chance)
    {
        if (double.IsNaN(chance))
        {
            return false;
        }

        if (chance <= 0)
        {
            return false;
        }

        if (chance >= 1)
        {
            return true;
        }

        return _rng.NextDouble() < chance;
    }
    public bool RollChanceInclusive(double chance)
    {
        if (double.IsNaN(chance))
        {
            return false;
        }

        if (chance <= 0)
        {
            // Inclusive roll: only succeeds on exactly 0.0, which depends on RNG implementation.
            return _rng.NextDouble() <= 0;
        }

        if (chance >= 1)
        {
            return true;
        }

        return _rng.NextDouble() <= chance;
    }


    public EggRecipient ChooseRecipient(double chanceForMale)
    {
        return RollChance(chanceForMale) ? EggRecipient.Male : EggRecipient.Female;
    }

    public string? ChooseEggCandidateOrDefault(IEnumerable<string?> candidates, string? fallback = null)
    {
        if (candidates is null)
        {
            throw new ArgumentNullException(nameof(candidates));
        }

        var list = candidates
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value!.Trim())
            .ToList();

        if (list.Count == 0)
        {
            return string.IsNullOrWhiteSpace(fallback) ? null : fallback.Trim();
        }

        return list[_rng.Next(list.Count)];
    }

    public T RollWeightedOrDefault<T>(IEnumerable<(T item, double weight)> entries, T fallback)
    {
        if (entries is null)
        {
            throw new ArgumentNullException(nameof(entries));
        }

        var list = entries as IList<(T item, double weight)> ?? entries.ToList();
        if (list.Count == 0)
        {
            return fallback;
        }

        var table = new WeightedTable<T>(_rng);
        foreach (var (item, weight) in list)
        {
            table.Add(item, weight);
        }

        return table.RollOrDefault(fallback);
    }
}
