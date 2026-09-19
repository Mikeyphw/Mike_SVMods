using System;
using System.Collections.Generic;
using System.Linq;
using LetMeHatch.Core.Breeding;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Locations;

namespace LetMeHatch.Infrastructure.Breeding;

/// <summary>
/// Adapts live Stardew Valley farm state into the pure <see cref="CoopBreedingIndex"/> model.
/// </summary>
public static class SmapiCoopBreedingIndexFactory
{
    public static CoopBreedingIndex Build(Farm farm)
    {
        if (farm is null) throw new ArgumentNullException(nameof(farm));

        var coopBuildings = farm.buildings
            .Where(IsCoopBuilding)
            .Where(b => b.indoors.Value is AnimalHouse)
            .ToList();

        var buildingIndex = new Dictionary<Building, int>(ReferenceEqualityComparer<Building>.Instance);
        var counts = new List<IDictionary<string, int>>(coopBuildings.Count);
        for (var i = 0; i < coopBuildings.Count; i++)
        {
            buildingIndex[coopBuildings[i]] = i;
            counts.Add(new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase));
        }

        for (var i = 0; i < coopBuildings.Count; i++)
        {
            var house = (AnimalHouse)coopBuildings[i].indoors.Value;
            foreach (var animal in house.animals.Values)
            {
                if (animal is null || !animal.isAdult()) continue;
                Increment(counts[i], animal.type.Value);
            }
        }

        foreach (var animal in farm.animals.Values)
        {
            if (animal is null || !animal.isAdult() || animal.home is null || !IsCoopBuilding(animal.home)) continue;
            if (animal.home is Building homeBuilding && buildingIndex.TryGetValue(homeBuilding, out var idx))
            {
                Increment(counts[idx], animal.type.Value);
            }
        }

        return CoopBreedingIndex.FromAdultCounts(counts);
    }

    private static void Increment(IDictionary<string, int> counts, string? typeKey)
    {
        if (string.IsNullOrWhiteSpace(typeKey)) return;
        counts[typeKey] = counts.TryGetValue(typeKey, out var existing) ? existing + 1 : 1;
    }

    private static bool IsCoopBuilding(Building building)
        => building.buildingType.Value?.IndexOf("coop", StringComparison.OrdinalIgnoreCase) >= 0;

    private sealed class ReferenceEqualityComparer<T> : IEqualityComparer<T> where T : class
    {
        public static readonly ReferenceEqualityComparer<T> Instance = new();
        public bool Equals(T? x, T? y) => ReferenceEquals(x, y);
        public int GetHashCode(T obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
    }
}
