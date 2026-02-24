using System;
using System.Collections.Generic;
using StardewValley.GameData.FarmAnimals;
using EggsInWild.Infrastructure.Config;
using EggsInWild.Core.ShopAnchors;

namespace EggsInWild.Core.ShopProgression;

/// <summary>
/// Determines whether a farm-animal key should be gated behind AnimalNotes progression in shop data.
/// </summary>
public static class AnimalShopProgressionPolicy
{
    private static readonly HashSet<string> DefaultUngatedVanilla =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "Chicken",
            "Duck",
            "Cow",
            "Goat",
            "Sheep",
            "Pig",
            "Rabbit",
        };

    public static bool DefaultEnabled(string animalId)
        => !DefaultUngatedVanilla.Contains(animalId);


    public static bool ShouldGate(
        string animalId,
        ModConfig config,
        IReadOnlyDictionary<string, FarmAnimalData> farmAnimals,
        ShopAnchorIndex anchors)
    {
        if (string.IsNullOrWhiteSpace(animalId))
            return false;

        if (config is null)
            throw new ArgumentNullException(nameof(config));

        if (!config.ProgressionMode)
            return false;

        if (string.Equals(animalId, "Blue Chicken", StringComparison.OrdinalIgnoreCase))
            return false;

        if (config.AnimalProgressionMode is not null
            && config.AnimalProgressionMode.TryGetValue(animalId, out var enabled))
        {
            return enabled;
        }

        return DefaultEnabled(animalId, farmAnimals, anchors);
    }

    public static bool DefaultEnabled(
        string animalId,
        IReadOnlyDictionary<string, FarmAnimalData> farmAnimals,
        ShopAnchorIndex anchors)
    {
        if (DefaultUngatedVanilla.Contains(animalId))
            return false;

        if (anchors?.HasAnchor(animalId) == true)
            return true;

        if (farmAnimals is not null
            && farmAnimals.TryGetValue(animalId, out var a)
            && a is not null
            && a.PurchasePrice < 0)
        {
            return true;
        }

        return true;
    }
}