using System;
using System.Collections.Generic;
using System.Linq;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Locations;

namespace LetMeHatch.Core.Breeding;

/// <summary>
/// Precomputed per-coop adult animal counts to support coop-scoped breeding decisions.
/// </summary>
public sealed class CoopBreedingIndex
{
    private readonly IReadOnlyList<CoopSnapshot> _coops;
    private readonly Dictionary<string, List<int>> _coopsByAdultType;
    private readonly Dictionary<(string MaleKey, string FemaleKey), int> _cache;

    private CoopBreedingIndex(IReadOnlyList<CoopSnapshot> coops, Dictionary<string, List<int>> coopsByAdultType)
    {
        _coops = coops;
        _coopsByAdultType = coopsByAdultType;
        _cache = new Dictionary<(string, string), int>(new PairKeyComparer());
    }

    public static CoopBreedingIndex Build(Farm farm)
    {
        if (farm is null) throw new ArgumentNullException(nameof(farm));

        var coopBuildings = farm.buildings
            .Where(IsCoopBuilding)
            .Where(b => b.indoors.Value is AnimalHouse)
            .ToList();

        var buildingIndex = new Dictionary<Building, int>(ReferenceEqualityComparer<Building>.Instance);
        var coops = new List<CoopSnapshot>(coopBuildings.Count);

        for (var i = 0; i < coopBuildings.Count; i++)
        {
            buildingIndex[coopBuildings[i]] = i;
            coops.Add(new CoopSnapshot(new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)));
        }

        for (var i = 0; i < coopBuildings.Count; i++)
        {
            var house = (AnimalHouse)coopBuildings[i].indoors.Value;
            foreach (var animal in house.animals.Values)
            {
                if (animal is null || !animal.isAdult())
                {
                    continue;
                }

                Increment(coops[i].AdultCounts, animal.type.Value);
            }
        }

        foreach (var animal in farm.animals.Values)
        {
            if (animal is null || !animal.isAdult())
            {
                continue;
            }

            if (animal.home is null || !IsCoopBuilding(animal.home))
            {
                continue;
            }

            if (animal.home is Building homeBuilding && buildingIndex.TryGetValue(homeBuilding, out var idx))
            {
                Increment(coops[idx].AdultCounts, animal.type.Value);
            }
        }

        var byType = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < coops.Count; i++)
        {
            foreach (var kvp in coops[i].AdultCounts)
            {
                if (!byType.TryGetValue(kvp.Key, out var list))
                {
                    list = new List<int>();
                    byType[kvp.Key] = list;
                }

                if (kvp.Value > 0)
                {
                    list.Add(i);
                }
            }
        }

        return new CoopBreedingIndex(coops, byType);
    }

    /// <summary>
    /// Gets the number of adult males of <paramref name="maleTypeKey"/> in coops that contain at least one adult
    /// animal of <paramref name="femaleTypeKey"/>.
    /// </summary>
    public int GetMaleQuantityForFemale(string maleTypeKey, string femaleTypeKey)
    {
        if (string.IsNullOrWhiteSpace(maleTypeKey) || string.IsNullOrWhiteSpace(femaleTypeKey))
        {
            return 0;
        }

        var key = (maleTypeKey, femaleTypeKey);
        if (_cache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        if (!_coopsByAdultType.TryGetValue(femaleTypeKey, out var femaleCoops) || femaleCoops.Count == 0)
        {
            _cache[key] = 0;
            return 0;
        }

        var sum = 0;
        foreach (var coopIdx in femaleCoops)
        {
            if (coopIdx < 0 || coopIdx >= _coops.Count)
            {
                continue;
            }

            if (_coops[coopIdx].AdultCounts.TryGetValue(maleTypeKey, out var count))
            {
                sum += count;
            }
        }

        _cache[key] = sum;
        return sum;
    }

    private static void Increment(IDictionary<string, int> counts, string? typeKey)
    {
        if (string.IsNullOrWhiteSpace(typeKey))
        {
            return;
        }

        counts[typeKey] = counts.TryGetValue(typeKey, out var existing) ? existing + 1 : 1;
    }

    private static bool IsCoopBuilding(Building building)
        => building.buildingType.Value?.IndexOf("coop", StringComparison.OrdinalIgnoreCase) >= 0;

    private sealed record CoopSnapshot(Dictionary<string, int> AdultCounts);

    private sealed class PairKeyComparer : IEqualityComparer<(string MaleKey, string FemaleKey)>
    {
        public bool Equals((string MaleKey, string FemaleKey) x, (string MaleKey, string FemaleKey) y)
            => string.Equals(x.MaleKey, y.MaleKey, StringComparison.OrdinalIgnoreCase)
                && string.Equals(x.FemaleKey, y.FemaleKey, StringComparison.OrdinalIgnoreCase);

        public int GetHashCode((string MaleKey, string FemaleKey) obj)
            => HashCode.Combine(
                StringComparer.OrdinalIgnoreCase.GetHashCode(obj.MaleKey ?? string.Empty),
                StringComparer.OrdinalIgnoreCase.GetHashCode(obj.FemaleKey ?? string.Empty)
            );
    }

    private sealed class ReferenceEqualityComparer<T> : IEqualityComparer<T> where T : class
    {
        public static readonly ReferenceEqualityComparer<T> Instance = new();

        public bool Equals(T? x, T? y) => ReferenceEquals(x, y);

        public int GetHashCode(T obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
    }
}
