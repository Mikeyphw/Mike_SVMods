using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using StardewValley.GameData.FarmAnimals;
using StardewValley.GameData.Objects;
using EggsInWild.Core.ShopAnchors;
using EggsInWild.Core.ShopProgression;
using EggsInWild.Core.Weighting;
using EggsInWild.Infrastructure.Config;
using Xunit;

namespace EggsInWild.Phase7.Tests;

public sealed class Phase7WeightsAndShopParityTests
{
    [Fact]
    public void ShopAnchorIndex_BuildsAnchorListingPriceFromParentPurchasePrice()
    {
        var farmAnimals = new Dictionary<string, FarmAnimalData>(StringComparer.OrdinalIgnoreCase)
        {
            ["Parent"] = new FarmAnimalData { PurchasePrice = 100 },
            ["Child"] = new FarmAnimalData { PurchasePrice = -1 },
        };

        SetAlternatePurchaseChildIds(farmAnimals["Parent"], new[] { "Child" });

        var anchors = ShopAnchorIndex.Build(farmAnimals);

        Assert.True(anchors.TryGetAnchorListingPrice("Child", out var listing));
        Assert.Equal(200, listing);
    }

    [Fact]
    public void EconomyWeights_DecreaseAsEffectiveValueIncreases()
    {
        var farmAnimals = new Dictionary<string, FarmAnimalData>(StringComparer.OrdinalIgnoreCase)
        {
            ["Low"] = new FarmAnimalData { PurchasePrice = 100 },
            ["High"] = new FarmAnimalData { PurchasePrice = 5000 },
        };

        var anchors = ShopAnchorIndex.Empty;
        var objects = new Dictionary<string, ObjectData>(StringComparer.OrdinalIgnoreCase);

        var calc = new WeggEconomyWeightCalculator(farmAnimals, anchors, objects, _ => false);
        var weights = calc.ComputeWeights(new[] { "Low", "High" });

        Assert.True(weights["High"] < weights["Low"]);
    }

    [Fact]
    public void EffectiveValue_UsesProduceEstimate_WhenNotPurchasableAndNoAnchor()
    {
        var farmAnimals = new Dictionary<string, FarmAnimalData>(StringComparer.OrdinalIgnoreCase)
        {
            ["Producer"] = new FarmAnimalData { PurchasePrice = -1, SellPrice = 0 },
        };

        SetInt(farmAnimals["Producer"], "DaysToMature", 2);
        SetInt(farmAnimals["Producer"], "DaysToProduce", 1);
        SetString(farmAnimals["Producer"], new[] { "HarvestedItemId", "HarvestedItemID", "ProduceItemId", "ProduceItemID" }, "1000");

        var obj = new ObjectData();
        SetInt(obj, "Price", 100);
        var objects = new Dictionary<string, ObjectData>(StringComparer.OrdinalIgnoreCase)
        {
            ["1000"] = obj,
        };

        var calc = new EffectiveValueCalculator(farmAnimals, ShopAnchorIndex.Empty, objects);
        Assert.True(calc.TryComputeEffectiveValue("Producer", out var v));

        // horizon=7, daysToMature=2 => productiveDays=5, daysToProduce=1 => cycles=5, price=100 => 500
        Assert.Equal(500, (int)Math.Round(v));
    }

    [Fact]
    public void Weights_AreClampedToMedianBounds()
    {
        var farmAnimals = new Dictionary<string, FarmAnimalData>(StringComparer.OrdinalIgnoreCase)
        {
            ["A"] = new FarmAnimalData { PurchasePrice = 100 },
            ["B"] = new FarmAnimalData { PurchasePrice = 100 },
            ["Outlier"] = new FarmAnimalData { PurchasePrice = 1_000_000 },
        };

        var calc = new WeggEconomyWeightCalculator(farmAnimals, ShopAnchorIndex.Empty, new Dictionary<string, ObjectData>(StringComparer.OrdinalIgnoreCase), _ => false);
        var weights = calc.ComputeWeights(farmAnimals.Keys);

        // Median raw weight is for PurchasePrice=100 => effectiveValue=200.
        var medianRaw = RawWeightFromValue(effectiveValue: 200.0);
        var minClamp = medianRaw / WeggWeightingDefaults.CapLow;

        Assert.True(weights["Outlier"] >= minClamp);
    }

    [Fact]
    public void UnlockedPenalty_ReducesWeight()
    {
        var farmAnimals = new Dictionary<string, FarmAnimalData>(StringComparer.OrdinalIgnoreCase)
        {
            ["Locked"] = new FarmAnimalData { PurchasePrice = 1000 },
            ["Unlocked"] = new FarmAnimalData { PurchasePrice = 1000 },
        };

        var calc = new WeggEconomyWeightCalculator(farmAnimals, ShopAnchorIndex.Empty, new Dictionary<string, ObjectData>(StringComparer.OrdinalIgnoreCase), id => id == "Unlocked");
        var weights = calc.ComputeWeights(farmAnimals.Keys);

        Assert.True(weights["Unlocked"] < weights["Locked"]);
        Assert.Equal(weights["Locked"] * WeggWeightingDefaults.UnlockedFactor, weights["Unlocked"], precision: 10);
    }

    [Fact]
    public void ApplyLockedEdits_SetsPurchasePriceMinusOne_AndRemovesFromAlternatePurchaseTypes()
    {
        var config = new ModConfig { ProgressionMode = true };

        var parent = new FarmAnimalData { PurchasePrice = 100 };
        var variant = new FarmAnimalData { PurchasePrice = 50 };

        SetAlternatePurchaseChildIds(parent, new[] { "Variant" });

        var farmAnimals = new Dictionary<string, FarmAnimalData>(StringComparer.OrdinalIgnoreCase)
        {
            ["Parent"] = parent,
            ["Variant"] = variant,
        };

        var anchors = ShopAnchorIndex.Build(farmAnimals);

        FarmAnimalShopProgressionEdits.ApplyLockedEdits(
            config: config,
            managedAnimalIds: new[] { "Variant" },
            farmAnimals: farmAnimals,
            anchors: anchors);

        Assert.Equal(-1, farmAnimals["Variant"].PurchasePrice);
        Assert.False(AlternatePurchaseTypesAccessor.ContainsAnimalId(parent, "Variant"));
    }

    private static double RawWeightFromValue(double effectiveValue)
    {
        var v = Math.Max(0.0, effectiveValue);
        var denom = Math.Pow(v + WeggWeightingDefaults.K, WeggWeightingDefaults.Alpha);
        return denom <= 0 || !double.IsFinite(denom) ? 0 : 1.0 / denom;
    }

    private static void SetAlternatePurchaseChildIds(FarmAnimalData parent, IReadOnlyList<string> childIds)
    {
        var prop = parent.GetType().GetProperty("AlternatePurchaseTypes", BindingFlags.Instance | BindingFlags.Public);
        if (prop is null)
            throw new InvalidOperationException("FarmAnimalData.AlternatePurchaseTypes property not found.");

        var listType = prop.PropertyType;

        // Build a new list instance.
        object listObj = Activator.CreateInstance(listType) ?? throw new InvalidOperationException("Could not create AlternatePurchaseTypes list.");

        // Determine element type.
        Type? elemType = null;
        if (listType.IsGenericType)
            elemType = listType.GetGenericArguments().FirstOrDefault();
        else if (listType.IsArray)
            elemType = listType.GetElementType();

        if (elemType is null)
            throw new InvalidOperationException("Could not determine AlternatePurchaseTypes element type.");

        // Create one entry object.
        var entry = Activator.CreateInstance(elemType) ?? throw new InvalidOperationException("Could not create AlternatePurchaseTypes entry.");

        SetString(entry, new[] { "Id", "ID" }, "Phase7_Test_Alt");
        SetString(entry, new[] { "Condition" }, "");
        SetStringList(entry, new[] { "AnimalIDs", "AnimalIds", "AnimalID", "AnimalId" }, childIds);

        // Add entry to list
        if (listObj is IList il)
        {
            il.Add(entry);
        }
        else
        {
            var add = listType.GetMethod("Add", BindingFlags.Instance | BindingFlags.Public);
            if (add is null)
                throw new InvalidOperationException("AlternatePurchaseTypes list does not support Add().");
            add.Invoke(listObj, new[] { entry });
        }

        prop.SetValue(parent, listObj);
    }

    private static void SetInt(object target, string name, int value)
    {
        var prop = target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
        if (prop is null || !prop.CanWrite)
            throw new InvalidOperationException($"Property {name} not found or not writable on {target.GetType().Name}.");

        prop.SetValue(target, value);
    }

    private static void SetString(object target, IReadOnlyList<string> names, string value)
    {
        foreach (var name in names)
        {
            var prop = target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
            if (prop is null || !prop.CanWrite)
                continue;

            prop.SetValue(target, value);
            return;
        }

        throw new InvalidOperationException($"No writable string property found among: {string.Join(", ", names)} on {target.GetType().Name}.");
    }

    private static void SetStringList(object target, IReadOnlyList<string> names, IReadOnlyList<string> values)
    {
        foreach (var name in names)
        {
            var prop = target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
            if (prop is null || !prop.CanWrite)
                continue;

            var t = prop.PropertyType;
            if (typeof(IList<string>).IsAssignableFrom(t))
            {
                prop.SetValue(target, values.ToList());
                return;
            }

            if (typeof(IList).IsAssignableFrom(t))
            {
                var list = Activator.CreateInstance(t) as IList;
                if (list is null)
                    continue;

                foreach (var v in values)
                    list.Add(v);

                prop.SetValue(target, list);
                return;
            }
        }

        throw new InvalidOperationException($"No writable list property found among: {string.Join(", ", names)} on {target.GetType().Name}.");
    }
}
