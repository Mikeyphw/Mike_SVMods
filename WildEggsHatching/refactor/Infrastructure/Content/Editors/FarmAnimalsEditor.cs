using System.Collections.Generic;
using System.Linq;
using StardewModdingAPI.Events;
using StardewValley.GameData.FarmAnimals;
using WildEggsHatching.Core.Enums;
using WildEggsHatching.Infrastructure.Content;

namespace WildEggsHatching.Infrastructure.Content.Editors;

/// <summary>
/// Editor for Data/FarmAnimals asset - modifies animal prices and adds eggs.
/// Refactored to use constants and proper DI patterns.
/// </summary>
public sealed class FarmAnimalsEditor : ProgressionAwareAssetEditor
{
    /// <inheritdoc />
    public override string AssetName => GameAssets.FarmAnimals;

    /// <inheritdoc />
    public override AssetEditPriority Priority => AssetEditPriority.Low;

    /// <summary>
    /// Maps vanilla animal IDs to their config price keys.
    /// </summary>
    private static readonly Dictionary<string, (string ConfigKey, int DefaultPrice)> VanillaAnimalPrices = new()
    {
        { WildEggConstants.VanillaAnimals.Dinosaur, ("DinoPrice", 6000) },
        { WildEggConstants.VanillaAnimals.VoidChicken, ("VoidChickenPrice", 2000) },
        { WildEggConstants.VanillaAnimals.Ostrich, ("OstrichPrice", 500) }
    };

    /// <inheritdoc />
    public FarmAnimalsEditor() : base() { }

    /// <inheritdoc />
    public FarmAnimalsEditor(
        PhaseProgressionService progressionService,
        IConfigProvider configProvider,
        IMonitor monitor)
        : base(progressionService, configProvider, monitor) { }

    /// <inheritdoc />
    public override void Edit<T>(IAssetData<T> asset, string assetName)
    {
        if (assetName != GameAssets.FarmAnimals)
            return;

        var animals = asset.AsDictionary<string, FarmAnimalData>().Data;

        // Always apply vanilla price modifications
        ApplyVanillaAnimalPrices(animals);

        // Part VI: Add wild eggs to animals
        if (HasReachedStep(ProgressionStep.PartVI))
        {
            AddWildEggsToAnimals(animals);
        }

        // Progression mode edits
        ApplyProgressionModeEdits(animals);

        Log($"FarmAnimalsEditor applied edits to {animals.Count} animals");
    }

    /// <summary>
    /// Applies price modifications to vanilla animals.
    /// </summary>
    private void ApplyVanillaAnimalPrices(IDictionary<string, FarmAnimalData> animals)
    {
        var config = GetConfig();

        foreach (var (animalId, priceInfo) in VanillaAnimalPrices)
        {
            if (!animals.TryGetValue(animalId, out var animalData))
                continue;

            int price = GetConfigPrice(
                config.GetType().GetProperty(priceInfo.ConfigKey)?
                    .GetValue(config)?.ToString(),
                priceInfo.DefaultPrice
            );

            animalData.SellPrice = price;

            // Special case for Void Chicken (purchase price)
            if (animalId == WildEggConstants.VanillaAnimals.VoidChicken)
            {
                animalData.PurchasePrice = price;
            }

            Log($"Updated {animalId} sell price to {price}", LogLevel.Trace);
        }
    }

    /// <summary>
    /// Adds wild eggs to animal data (Part VI - final step).
    /// </summary>
    private void AddWildEggsToAnimals(IDictionary<string, FarmAnimalData> animals)
    {
        foreach (var (id, data) in animals)
        {
            string? buildingType = data.BuildingType;
            if (string.IsNullOrEmpty(buildingType))
                continue;

            // Determine building category
            bool isCoop = buildingType == WildEggConstants.Buildings.Coop 
                       || buildingType == WildEggConstants.Buildings.BigCoop;
            bool isBarn = buildingType == WildEggConstants.Buildings.Barn 
                       || buildingType == WildEggConstants.Buildings.BigBarn;

            if (!isCoop && !isBarn)
                continue;

            // Initialize egg list if needed
            data.EggItemIds ??= new List<string>();
            data.IncubationTime = 1000; // Wild eggs take longer

            // Add location-specific eggs
            string prefix = WildEggConstants.ItemPrefix;
            string location = isCoop ? "Coop" : "Barn";

            data.EggItemIds.Add($"{prefix}{location}WEgg");
            data.EggItemIds.Add($"{prefix}{location}LWEgg");
            data.EggItemIds.Add($"{prefix}{location}MWEgg");
            data.EggItemIds.Add($"{prefix}{location}LMWEgg");

            Log($"Added wild eggs to {id} ({location})", LogLevel.Trace);
        }
    }

    /// <summary>
    /// Applies progression mode edits - hides/shows animals based on unlocked state.
    /// </summary>
    private void ApplyProgressionModeEdits(IDictionary<string, FarmAnimalData> animals)
    {
        if (!IsProgressionModeEnabled())
            return;

        var config = GetConfig();
        var progressionMode = config.AnimalProgressionMode;

        if (progressionMode == null || progressionMode.Count == 0)
            return;

        foreach (var (animalId, isEnabled) in progressionMode)
        {
            if (!animals.TryGetValue(animalId, out var animalData))
                continue;

            if (isEnabled)
            {
                // Animal is enabled - restore price if hidden
                if (animalData.PurchasePrice < 0)
                {
                    // Would need original data tracking - log for now
                    Log($"Animal {animalId} is enabled but was hidden - needs restore logic", LogLevel.Warn);
                }
            }
            else
            {
                // Animal is disabled - hide from shop
                animalData.PurchasePrice = -1;
                Log($"Hidden {animalId} from purchase", LogLevel.Trace);
            }
        }
    }

    /// <inheritdoc />
    public override bool ShouldApply()
    {
        return true; // Always apply for basic edits
    }
}
