using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RoosterAddon.Application.FarmAnimals;
using RoosterAddon.Enums;
using RoosterAddon.Framework.Config;
using Mikew.BirdsOfAFeather.Models;
using StardewValley;
using StardewValley.GameData.FarmAnimals;

namespace RoosterAddon.Application.Planning;

/// <summary>
/// Computes and fingerprints a daily hatching plan.
/// </summary>
internal sealed class DailyHatchingPlanService
{
    private readonly FarmAnimalBuildingAndLocationRulesService _buildingRules = new();

    public string ComputeFingerprint(
        RoosterAddonConfig config,
        IReadOnlyDictionary<RoosterType, RoosterData> roosters,
        IReadOnlyDictionary<string, Dictionary<EggTypeEntries, string>> femaleEggs,
        object dailyLuck,
        long farmId,
        int day
    )
    {
        if (config is null) throw new ArgumentNullException(nameof(config));
        if (roosters is null) throw new ArgumentNullException(nameof(roosters));
        if (femaleEggs is null) throw new ArgumentNullException(nameof(femaleEggs));

        var sb = new StringBuilder();

        sb.Append("v1|");
        sb.Append(farmId).Append('|').Append(day).Append('|');
        sb.Append(dailyLuck).Append('|');

        // General settings used directly by hatching/rolling.
        var g = config.General;
        sb.Append(g.HatchingMode).Append('|')
          .Append(g.EnableEggProbability).Append('|')
          .Append(g.EnableLargeEggProbability).Append('|')
          .Append(g.RoosterToChickenRatio).Append('|')
          .Append(g.EggProbability).Append('|')
          .Append(g.LargeEggProbability).Append('|')
          .Append(g.FertilizedEggProbability).Append('|')
          .Append(g.ChosenPercentageDailyLuckBonus).Append('|');

        // Per-rooster: include rolled eggs/probs + female assignments.
        foreach (var rooster in roosters.OrderBy(r => r.Key).Select(r => r.Value))
        {
            sb.Append("R|").Append(rooster.RoosterType).Append('|')
              .Append(rooster.FarmAnimalName).Append('|')
              .Append(rooster.Female).Append('|')
              .Append(rooster.BuildingRequired).Append('|')
              .Append(rooster.Quantity).Append('|');

            foreach (var eggType in rooster.Eggs.OrderBy(k => k.Key))
            {
                sb.Append("E|").Append(eggType.Key).Append('|');
                foreach (var kvp in eggType.Value.OrderBy(k => k.Key))
                    sb.Append(kvp.Key).Append('=').Append(kvp.Value).Append(';');
                sb.Append('|');
            }

            var females = rooster.Females?.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray() ?? Array.Empty<string>();
            sb.Append("F|").Append(string.Join(",", females)).Append('|');
        }

        return StableHash.Hex(StableHash.Fnv1a64(sb.ToString()));
    }

    public DailyHatchingPlanEnvelope BuildPlan(
        IDictionary<string, FarmAnimalData> farmAnimalsData,
        RoosterAddonConfig config,
        IReadOnlyDictionary<RoosterType, RoosterData> roosters,
        IReadOnlyDictionary<string, Dictionary<EggTypeEntries, string>> femaleEggs,
        object dailyLuck,
        long farmId,
        int day,
        long nonce
    )
    {
        if (farmAnimalsData is null) throw new ArgumentNullException(nameof(farmAnimalsData));
        if (config is null) throw new ArgumentNullException(nameof(config));
        if (roosters is null) throw new ArgumentNullException(nameof(roosters));
        if (femaleEggs is null) throw new ArgumentNullException(nameof(femaleEggs));

        var fingerprint = ComputeFingerprint(config, roosters, femaleEggs, dailyLuck, farmId, day);

        var plan = new DailyHatchingPlanEnvelope
        {
            FarmId = farmId,
            Day = day,
            Fingerprint = fingerprint,
            Nonce = nonce,
        };

        foreach (var rooster in roosters.Values)
        {
            var buildingKey = ResolveBuildingKey(rooster, farmAnimalsData);
            if (!plan.Buildings.TryGetValue(buildingKey, out var building))
            {
                building = new BuildingHatchingPlan { BuildingKey = buildingKey };
                plan.Buildings[buildingKey] = building;
            }

            if (!building.Roosters.TryGetValue(rooster.FarmAnimalName, out var roosterPlan))
            {
                roosterPlan = new RoosterHatchingPlan { RoosterKey = rooster.FarmAnimalName };
                building.Roosters[rooster.FarmAnimalName] = roosterPlan;
            }

            foreach (var femaleKey in rooster.Females)
            {
                if (!roosterPlan.Females.TryGetValue(femaleKey, out var femalePlan))
                {
                    femalePlan = new FemaleHatchingPlan { FemaleKey = femaleKey };
                    roosterPlan.Females[femaleKey] = femalePlan;
                }

                if (config.General.HatchingMode == HatchingMode.OnlyFertilizedEggs)
                    femalePlan.FertilizedOnly = BuildFertilizedOnlyPlan(rooster, femaleKey, farmAnimalsData, config, femaleEggs, farmId, day, buildingKey, nonce);
                else
                {
                    femalePlan.NormalEgg = BuildEggChancePlan(rooster, femaleKey, EggTypeEntries.Normal, farmAnimalsData, config, femaleEggs, farmId, day, buildingKey, nonce);
                    femalePlan.LargeEgg = BuildEggChancePlan(rooster, femaleKey, EggTypeEntries.Large, farmAnimalsData, config, femaleEggs, farmId, day, buildingKey, nonce);
                }
            }
        }

        return plan;
    }

    private static EggChancePlan? BuildEggChancePlan(
        RoosterData rooster,
        string femaleKey,
        EggTypeEntries eggType,
        IDictionary<string, FarmAnimalData> data,
        RoosterAddonConfig config,
        IReadOnlyDictionary<string, Dictionary<EggTypeEntries, string>> femaleEggs,
        long farmId,
        int day,
        string buildingKey,
        long nonce
    )
    {
        var enabled = eggType == EggTypeEntries.Normal
            ? config.General.EnableEggProbability
            : config.General.EnableLargeEggProbability;

        if (!enabled)
            return null;

        if (!rooster.Eggs.TryGetValue(eggType, out var egg)
            || !egg.TryGetValue(RoosterItemDataEntries.Probability, out var rawChance)
            || !int.TryParse(rawChance, out var chance)
            || chance <= 0)
        {
            return new EggChancePlan { Enabled = true, ChancePercent = 0, RolledSuccess = false };
        }

        if (!egg.TryGetValue(RoosterItemDataEntries.Name, out var eggId) || string.IsNullOrWhiteSpace(eggId))
            return new EggChancePlan { Enabled = true, ChancePercent = chance, RolledSuccess = false };

        femaleEggs.TryGetValue(femaleKey, out var femaleEggs);
        femaleEggs?.TryGetValue(eggType, out var femaleEggIdToRemove);

        var rollKey = $"egg|{buildingKey}|{rooster.FarmAnimalName}|{femaleKey}|{eggType}|{nonce}";
        var rolled = RollPercentInt(farmId, day, rollKey, chance);

        return new EggChancePlan
        {
            Enabled = true,
            ChancePercent = chance,
            RolledSuccess = rolled,
            EggIdToAddToRooster = eggId,
            FemaleEggIdToRemove = femaleEggIdToRemove
        };
    }

    private static FertilizedOnlyPlan? BuildFertilizedOnlyPlan(
        RoosterData rooster,
        string femaleKey,
        IDictionary<string, FarmAnimalData> data,
        RoosterAddonConfig config,
        IReadOnlyDictionary<string, Dictionary<EggTypeEntries, string>> femaleEggs,
        long farmId,
        int day,
        string buildingKey,
        long nonce
    )
    {
        if (!rooster.Eggs.TryGetValue(EggTypeEntries.Fertilized, out var egg)
            || !egg.TryGetValue(RoosterItemDataEntries.Probability, out var rawChance)
            || !float.TryParse(rawChance, out var fertilizedChance))
        {
            fertilizedChance = 0;
        }

        if (!egg.TryGetValue(RoosterItemDataEntries.Name, out var roosterFertilizedEggId)
            || string.IsNullOrWhiteSpace(roosterFertilizedEggId))
        {
            return null;
        }

        if (!femaleEggs.TryGetValue(femaleKey, out var femaleEggs)
            || !femaleEggs.TryGetValue(EggTypeEntries.Fertilized, out var femaleFertilizedEggId)
            || string.IsNullOrWhiteSpace(femaleFertilizedEggId))
        {
            return null;
        }

        var upgradeKey = $"fert-up|{buildingKey}|{rooster.FarmAnimalName}|{femaleKey}|{nonce}";
        var ratioKey = $"fert-ratio|{buildingKey}|{rooster.FarmAnimalName}|{femaleKey}|{nonce}";

        var upgrade = RollPercentFloat(farmId, day, upgradeKey, fertilizedChance);
        var roosterHatches = RollPercentInt(farmId, day, ratioKey, config.General.RoosterToChickenRatio);

        return new FertilizedOnlyPlan
        {
            FemaleUpgradeChancePercent = fertilizedChance,
            RoosterToChickenRatioPercent = config.General.RoosterToChickenRatio,
            UpgradeFemaleProduceToFertilized = upgrade,
            RoosterHatchesFertilizedEgg = roosterHatches,
            RoosterFertilizedEggId = roosterFertilizedEggId,
            FemaleFertilizedEggId = femaleFertilizedEggId
        };
    }

    private static bool RollPercentInt(long farmId, int day, string rollKey, int chancePercent)
    {
        if (chancePercent <= 0) return false;
        if (chancePercent >= 100) return true;

        var h = StableHash.Fnv1a64($"{farmId}|{day}|{rollKey}");
        var v = (int)(h % 100UL);
        return v < chancePercent;
    }

    private static bool RollPercentFloat(long farmId, int day, string rollKey, float chancePercent)
    {
        if (chancePercent <= 0f) return false;
        if (chancePercent >= 100f) return true;

        var h = StableHash.Fnv1a64($"{farmId}|{day}|{rollKey}");
        // 0..9999
        var v = (int)(h % 10000UL);
        var threshold = (int)Math.Round(chancePercent * 100f);
        return v < threshold;
    }

    private static string ResolveBuildingKey(RoosterData rooster, IDictionary<string, FarmAnimalData> data)
    {
        if (data.TryGetValue(rooster.FarmAnimalName, out var entry) && !string.IsNullOrWhiteSpace(entry.RequiredBuilding))
            return entry.RequiredBuilding.Trim();

        // Best effort: match game keys used by FarmAnimalBuildingAndLocationRulesService.
        return rooster.BuildingRequired switch
        {
            RequiredBuilding.Coop => "Coop",
            RequiredBuilding.BigCoop => "Big Coop",
            RequiredBuilding.DeluxeCoop => "Deluxe Coop",
            _ => "Big Coop",
        };
    }
}
