using System;
using System.Collections.Generic;
using System.Reflection;
using StardewValley.GameData.FarmAnimals;
using StardewValley.GameData.Objects;
using WildEggsHatching.Core.ShopAnchors;
using WildEggsHatching.Core.Weighting;
using Xunit;

namespace WildEggsHatching.Refactor.Tests;

public sealed class WeightingPhase4Tests
{
    [Fact]
    public void DeterministicPicker_SameSeed_SamePick()
    {
        var items = new[] { "A", "B", "C" };
        var weights = new[] { 1.0, 3.0, 6.0 };

        Assert.True(DeterministicWeightedPicker.TryPick(items, weights, seed: 123, out var p1));
        Assert.True(DeterministicWeightedPicker.TryPick(items, weights, seed: 123, out var p2));
        Assert.Equal(p1, p2);
    }

    [Fact]
    public void DeterministicPicker_AllZeroWeights_PicksFirst()
    {
        var items = new[] { "A", "B", "C" };
        var weights = new[] { 0.0, -1.0, double.NaN };

        Assert.True(DeterministicWeightedPicker.TryPick(items, weights, seed: 123, out var picked));
        Assert.Equal("A", picked);
    }

    [Fact]
    public void EffectiveValueCalculator_ProduceEstimate_UsesObjectPrices()
    {
        var farm = new Dictionary<string, FarmAnimalData>(StringComparer.OrdinalIgnoreCase)
        {
            ["Test"] = new FarmAnimalData { PurchasePrice = 0, SellPrice = 0 },
        };

        SetIntProperty(farm["Test"], 0, "DaysToMature");
        SetIntProperty(farm["Test"], 1, "DaysToProduce");
        SetStringProperty(farm["Test"], "Egg", "HarvestedItemId", "ProduceItemId");

        var objects = new Dictionary<string, ObjectData>(StringComparer.OrdinalIgnoreCase)
        {
            ["Egg"] = NewObjectDataWithPrice(50),
        };

        var calc = new EffectiveValueCalculator(farm, ShopAnchorIndex.Empty, objects, horizonDays: 7, deluxeProbability: 0.0);

        Assert.True(calc.TryComputeEffectiveValue("Test", out var v));
        Assert.Equal(350.0, v, precision: 6);
    }

    [Fact]
    public void EconomyWeightCalculator_AppliesPenalties_AndClamps()
    {
        var farm = new Dictionary<string, FarmAnimalData>(StringComparer.OrdinalIgnoreCase)
        {
            ["Cheap"] = new FarmAnimalData { PurchasePrice = 50, SellPrice = 0 },   // value=100
            ["Expensive"] = new FarmAnimalData { PurchasePrice = 50000, SellPrice = 0 }, // value=100000
            ["Hidden"] = new FarmAnimalData { PurchasePrice = -1, SellPrice = 100 }, // value=100 + hidden penalty
            ["Unlocked"] = new FarmAnimalData { PurchasePrice = 50, SellPrice = 0 }, // value=100 + unlocked penalty
        };

        var objects = new Dictionary<string, ObjectData>(StringComparer.OrdinalIgnoreCase);

        var calc = new WeggEconomyWeightCalculator(
            farm,
            ShopAnchorIndex.Empty,
            objects,
            isUnlocked: id => string.Equals(id, "Unlocked", StringComparison.OrdinalIgnoreCase));

        var weights = calc.ComputeWeights(new[] { "Cheap", "Expensive", "Hidden", "Unlocked" });

        Assert.True(weights["Cheap"] > 0);
        Assert.True(weights["Expensive"] > 0);

        // Hidden penalty reduces weight.
        Assert.True(weights["Hidden"] < weights["Cheap"]);

        // Unlocked penalty reduces weight.
        Assert.True(weights["Unlocked"] < weights["Cheap"]);

        // Clamp should keep within bounds derived from median.
        var ws = new List<double> { weights["Cheap"], weights["Expensive"], weights["Hidden"], weights["Unlocked"] };
        var median = GetMedian(ws);
        var minW = median / WeggWeightingDefaults.CapLow;
        var maxW = median * WeggWeightingDefaults.CapHigh;

        foreach (var w in ws)
            Assert.InRange(w, minW - 1e-12, maxW + 1e-12);
    }

    private static double GetMedian(List<double> values)
    {
        values.Sort();
        return values.Count % 2 == 1
            ? values[values.Count / 2]
            : (values[values.Count / 2 - 1] + values[values.Count / 2]) / 2.0;
    }

    private static ObjectData NewObjectDataWithPrice(int price)
    {
        var obj = new ObjectData();
        SetIntProperty(obj, price, "Price", "SellPrice");
        return obj;
    }

    private static void SetIntProperty(object target, int value, params string[] names)
    {
        foreach (var name in names)
        {
            var prop = target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
            if (prop is null || !prop.CanWrite)
                continue;

            if (prop.PropertyType == typeof(int))
            {
                prop.SetValue(target, value);
                return;
            }

            if (prop.PropertyType == typeof(long))
            {
                prop.SetValue(target, (long)value);
                return;
            }
        }

        throw new InvalidOperationException($"Expected a writable int property among: {string.Join(", ", names)} on {target.GetType().Name}.");
    }

    private static void SetStringProperty(object target, string value, params string[] names)
    {
        foreach (var name in names)
        {
            var prop = target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
            if (prop is null || !prop.CanWrite)
                continue;

            if (prop.PropertyType == typeof(string))
            {
                prop.SetValue(target, value);
                return;
            }
        }

        throw new InvalidOperationException($"Expected a writable string property among: {string.Join(", ", names)} on {target.GetType().Name}.");
    }
}
