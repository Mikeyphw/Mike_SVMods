using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using StardewValley.GameData.FarmAnimals;
using StardewValley.GameData.Objects;
using WildEggsHatching.Core.ShopAnchors;

namespace WildEggsHatching.Core.Weighting;

/// <summary>
/// Computes an economy-derived "effective value" for farm animals (higher value => lower spawn weight).
/// </summary>
public sealed class EffectiveValueCalculator
{
    private static readonly StringComparer KeyComparer = StringComparer.OrdinalIgnoreCase;

    private readonly IReadOnlyDictionary<string, FarmAnimalData> _farmAnimals;
    private readonly ShopAnchorIndex _anchors;
    private readonly IReadOnlyDictionary<string, ObjectData> _objects;
    private readonly int _horizonDays;
    private readonly double _pDeluxe;

    public EffectiveValueCalculator(
        IReadOnlyDictionary<string, FarmAnimalData> farmAnimals,
        ShopAnchorIndex anchors,
        IReadOnlyDictionary<string, ObjectData> objects,
        int horizonDays = WeggWeightingDefaults.HorizonDays,
        double deluxeProbability = WeggWeightingDefaults.DeluxeProbability)
    {
        _farmAnimals = farmAnimals ?? throw new ArgumentNullException(nameof(farmAnimals));
        _anchors = anchors ?? throw new ArgumentNullException(nameof(anchors));
        _objects = objects ?? throw new ArgumentNullException(nameof(objects));
        _horizonDays = Math.Max(1, horizonDays);
        _pDeluxe = Math.Clamp(deluxeProbability, 0.0, 1.0);
    }

    /// <summary>
    /// Compute effective values for a candidate set, filling any missing/unknown entries with the median
    /// effective value from known candidates.
    /// </summary>
    public IReadOnlyDictionary<string, double> ComputeForCandidates(IEnumerable<string> candidateIds)
    {
        if (candidateIds is null)
            throw new ArgumentNullException(nameof(candidateIds));

        var ids = candidateIds
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(KeyComparer)
            .ToList();

        var raw = new Dictionary<string, double>(KeyComparer);
        var known = new List<double>(capacity: ids.Count);

        foreach (var id in ids)
        {
            var value = TryComputeEffectiveValue(id, out var v) ? v : 0.0;
            raw[id] = value;
            if (value > 0)
                known.Add(value);
        }

        var median = MedianMath.MedianOrDefault(known, defaultValue: 1.0);

        var filled = new Dictionary<string, double>(KeyComparer);
        foreach (var id in ids)
        {
            var v = raw[id];
            filled[id] = v > 0 ? v : median;
        }

        return filled;
    }

    /// <summary>Try compute a single animal's effective value using the defined priority order.</summary>
    public bool TryComputeEffectiveValue(string animalId, out double value)
    {
        value = 0;

        if (string.IsNullOrWhiteSpace(animalId))
            return false;

        if (!_farmAnimals.TryGetValue(animalId, out var animal) || animal is null)
            return false;

        // 1) PurchasePrice>0 -> 2*PurchasePrice (shop listing price)
        if (animal.PurchasePrice > 0)
        {
            value = animal.PurchasePrice * 2.0;
            return value > 0;
        }

        // 2) Anchored variant -> anchor listing price
        if (_anchors.TryGetAnchorListingPrice(animalId, out var anchorListing))
        {
            value = anchorListing;
            return value > 0;
        }

        // 3) SellPrice>0 -> SellPrice
        if (animal.SellPrice > 0)
        {
            value = animal.SellPrice;
            return value > 0;
        }

        // 4) Produce estimate (maturity/production horizon)
        if (TryEstimateProduceValue(animal, out var produceValue))
        {
            value = produceValue;
            return value > 0;
        }

        return false;
    }

    private bool TryEstimateProduceValue(FarmAnimalData animal, out double estimatedValue)
    {
        estimatedValue = 0;

        if (!TryGetInt(animal, out var daysToMature, "DaysToMature"))
            return false;

        if (!TryGetInt(animal, out var daysToProduce, "DaysToProduce"))
            return false;

        if (daysToProduce <= 0)
            return false;

        var baseProduceId = TryGetFirstString(animal,
            "HarvestedItemId", "HarvestedItemID", "ProduceItemId", "ProduceItemID", "ProduceItemIds", "ProduceItemIDs");

        if (string.IsNullOrWhiteSpace(baseProduceId))
            return false;

        var deluxeProduceId = TryGetFirstString(animal,
            "DeluxeHarvestedItemId", "DeluxeHarvestedItemID", "DeluxeProduceItemId", "DeluxeProduceItemID",
            "DeluxeProduceItemIds", "DeluxeProduceItemIDs");

        if (!TryGetObjectSellPrice(baseProduceId, out var basePrice))
            return false;

        var deluxePrice = 0;
        var deluxeOk = !string.IsNullOrWhiteSpace(deluxeProduceId) && TryGetObjectSellPrice(deluxeProduceId!, out deluxePrice);

        var expectedPerProduce = deluxeOk
            ? basePrice * (1.0 - _pDeluxe) + deluxePrice * _pDeluxe
            : basePrice;

        if (expectedPerProduce <= 0)
            return false;

        var productiveDays = Math.Max(0, _horizonDays - Math.Max(0, daysToMature));
        var cycles = productiveDays / daysToProduce;

        if (cycles <= 0)
            return false;

        estimatedValue = expectedPerProduce * cycles;
        return estimatedValue > 0;
    }

    private bool TryGetObjectSellPrice(string objectId, out int price)
    {
        price = 0;

        if (string.IsNullOrWhiteSpace(objectId))
            return false;

        if (!_objects.TryGetValue(objectId, out var obj) || obj is null)
            return false;

        return ObjectPriceResolver.TryGetSellPrice(obj, out price) && price > 0;
    }

    private static bool TryGetInt(object obj, out int value, params string[] propNames)
    {
        value = 0;
        if (obj is null)
            return false;

        foreach (var name in propNames)
        {
            var prop = obj.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
            if (prop is null)
                continue;

            var v = prop.GetValue(obj);
            if (v is int i)
            {
                value = i;
                return true;
            }

            if (v is long l)
            {
                value = (int)l;
                return true;
            }
        }

        return false;
    }

    private static string? TryGetFirstString(object obj, params string[] propNames)
    {
        foreach (var name in propNames)
        {
            var prop = obj.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
            if (prop is null)
                continue;

            var v = prop.GetValue(obj);

            if (v is string s && !string.IsNullOrWhiteSpace(s))
                return s;

            if (v is System.Collections.IEnumerable enumerable)
            {
                foreach (var item in enumerable)
                {
                    if (item is string es && !string.IsNullOrWhiteSpace(es))
                        return es;
                }
            }
        }

        return null;
    }
}
