using System;
using System.Collections.Generic;
using System.Linq;
using RoosterAddon.Constants;
using RoosterAddon.Framework.Config;
using StardewModdingAPI;
using StardewValley.GameData.FarmAnimals;

namespace RoosterAddon.Application;

internal sealed partial class RoosterCoordinator
{
    private void RunStartupValidationV2()
    {
        if (!Context.IsWorldReady)
            return;

        var errors = new List<string>();
        var warnings = new List<string>();

        ValidatePercentRange(Config.General.EggProbability, 0, 100, "General.EggProbability", errors);
        ValidatePercentRange(Config.General.LargeEggProbability, 0, 100, "General.LargeEggProbability", errors);
        ValidatePercentRange(Config.General.FertilizedEggProbability, 0, 100, "General.FertilizedEggProbability", errors);
        ValidatePercentRange(Config.General.RoosterToChickenRatio, 0, 100, "General.RoosterToChickenRatio", errors);
        ValidatePercentRange(Config.General.ChosenPercentageDailyLuckBonus, 0, 100, "General.ChosenPercentageDailyLuckBonus", errors);

        if (Config.General.BoostByRooster < 0f || Config.General.BoostByRooster > 1000f)
            errors.Add("General.BoostByRooster must be between 0 and 1000.");

        foreach (var (rt, settings) in Config.Roosters)
        {
            if (settings.MatureDays < 0) errors.Add($"Roosters[{rt}].MatureDays must be >= 0.");
            if (settings.ProduceDays < 0) errors.Add($"Roosters[{rt}].ProduceDays must be >= 0.");
            if (settings.Price < 0) errors.Add($"Roosters[{rt}].Price must be >= 0.");

            ValidateProduceEntries(rt, "ProduceEntries", settings.ProduceOption == ProduceMode.CustomizeBelow, settings.ProduceEntries, errors);
            ValidateProduceEntries(rt, "DeluxeProduceEntries", settings.DeluxeProduceOption == DeluxeProduceMode.CustomizeBelow, settings.DeluxeProduceEntries, errors);
        }

        Dictionary<string, FarmAnimalData>? farmAnimals = null;
        try
        {
            farmAnimals = Helper.GameContent.Load<Dictionary<string, FarmAnimalData>>(FarmAnimalsAssetName);
        }
        catch (Exception ex)
        {
            errors.Add($"Failed to load {FarmAnimalsAssetName}: {ex.Message}");
        }

        if (farmAnimals is not null)
        {
            foreach (var key in new[] { RoosterKeys.White, RoosterKeys.Brown, RoosterKeys.Blue, RoosterKeys.Void, RoosterKeys.Gold })
            {
                if (!farmAnimals.ContainsKey(key))
                    errors.Add($"Missing required FarmAnimals key: '{key}'.");
            }

            foreach (var (femaleKey, assignment) in Config.FemalesOptions)
            {
                if (assignment == FemaleRoosterAssignment.Off)
                    continue;

                if (!farmAnimals.ContainsKey(femaleKey))
                    warnings.Add($"FemalesOptions key '{femaleKey}' not found in {FarmAnimalsAssetName} (assigned {assignment}).");
            }
        }

        if (warnings.Count > 0)
        {
            Monitor.Log("RoosterAddon startup validation warnings:", LogLevel.Warn);
            foreach (var w in warnings)
                Monitor.Log($"  - {w}", LogLevel.Warn);
        }

        if (errors.Count == 0)
            return;

        Monitor.Log("RoosterAddon startup validation errors (fatal):", LogLevel.Error);
        foreach (var err in errors)
            Monitor.Log($"  - {err}", LogLevel.Error);

        throw new InvalidOperationException("RoosterAddon configuration/data is invalid. See SMAPI log for details.");
    }

    private static void ValidatePercentRange(int value, int min, int max, string name, List<string> errors)
    {
        if (value < min || value > max)
            errors.Add($"{name} must be between {min} and {max}.");
    }

    private static void ValidateProduceEntries(
        Enums.RoosterType roosterType,
        string fieldName,
        bool enabled,
        List<ProduceEntry> entries,
        List<string> errors
    )
    {
        if (!enabled)
            return;

        if (entries is null || entries.Count == 0)
        {
            errors.Add($"Roosters[{roosterType}].{fieldName} must contain at least 1 entry when CustomizeBelow.");
            return;
        }

        var sum = 0;
        for (var i = 0; i < entries.Count; i++)
        {
            var p = entries[i].Probability;
            if (p < 0 || p > 100)
                errors.Add($"Roosters[{roosterType}].{fieldName}[{i}].Probability must be 0..100.");
            sum += Math.Max(0, p);

            var choice = entries[i].Choice?.Trim() ?? string.Empty;
            var custom = entries[i].CustomItemName?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(choice) && string.IsNullOrWhiteSpace(custom))
                errors.Add($"Roosters[{roosterType}].{fieldName}[{i}] must have a Choice or CustomItemName.");
        }

        if (sum <= 0)
            errors.Add($"Roosters[{roosterType}].{fieldName} probabilities sum to 0; at least one must be > 0.");
    }
}
