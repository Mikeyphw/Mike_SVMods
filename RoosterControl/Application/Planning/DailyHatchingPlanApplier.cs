using System;
using System.Collections.Generic;
using System.Linq;
using RoosterControl.Constants;
using RoosterControl.Enums;
using RoosterControl.Helpers;
using StardewValley.GameData.FarmAnimals;

namespace RoosterControl.Application.Planning;

/// <summary>
/// Applies a computed daily plan to <c>Data/FarmAnimals</c>.
/// </summary>
internal sealed class DailyHatchingPlanApplier
{
    public void Apply(
        DailyHatchingPlanEnvelope plan,
        string buildingKey,
        RoosterData rooster,
        IDictionary<string, FarmAnimalData> data,
        IReadOnlyList<string> otherAnimalsSharingEggs
    )
    {
        if (plan is null) throw new ArgumentNullException(nameof(plan));
        if (rooster is null) throw new ArgumentNullException(nameof(rooster));
        if (data is null) throw new ArgumentNullException(nameof(data));

        if (!plan.Buildings.TryGetValue(buildingKey, out var building))
            return;

        if (!building.Roosters.TryGetValue(rooster.FarmAnimalName, out var roosterPlan))
            return;

        foreach (var femaleKey in rooster.Females)
        {
            if (!roosterPlan.Females.TryGetValue(femaleKey, out var femalePlan))
                continue;

            if (femalePlan.FertilizedOnly is not null)
                ApplyFertilizedOnly(femaleKey, rooster, data, otherAnimalsSharingEggs, femalePlan.FertilizedOnly);
            else
            {
                ApplyEggChance(femaleKey, rooster, data, otherAnimalsSharingEggs, femalePlan.NormalEgg);
                ApplyEggChance(femaleKey, rooster, data, otherAnimalsSharingEggs, femalePlan.LargeEgg);
            }
        }
    }

    private static void ApplyEggChance(
        string femaleKey,
        RoosterData rooster,
        IDictionary<string, FarmAnimalData> data,
        IReadOnlyList<string> otherAnimalsSharingEggs,
        EggChancePlan? plan
    )
    {
        if (plan is null || !plan.Enabled || !plan.RolledSuccess)
            return;

        if (!data.ContainsKey(rooster.FarmAnimalName) || !data.ContainsKey(femaleKey))
            return;

        var eggId = plan.EggIdToAddToRooster;
        if (string.IsNullOrWhiteSpace(eggId))
            return;

        data[rooster.FarmAnimalName].EggItemIds ??= new();
        if (!data[rooster.FarmAnimalName].EggItemIds.Contains(eggId, StringComparer.OrdinalIgnoreCase))
            data[rooster.FarmAnimalName].EggItemIds.Add(eggId);

        if (!string.IsNullOrWhiteSpace(plan.FemaleEggIdToRemove))
        {
            data[femaleKey].EggItemIds ??= new();
            RemoveEggItemIdIgnoreCase(data[femaleKey].EggItemIds, plan.FemaleEggIdToRemove);
        }

        foreach (var otherAnimal in otherAnimalsSharingEggs)
        {
            if (!data.ContainsKey(otherAnimal) || data[otherAnimal].EggItemIds is null)
                continue;

            RemoveEggItemIdIgnoreCase(data[otherAnimal].EggItemIds, eggId);
        }
    }

    private static void ApplyFertilizedOnly(
        string femaleKey,
        RoosterData rooster,
        IDictionary<string, FarmAnimalData> data,
        IReadOnlyList<string> otherAnimalsSharingEggs,
        FertilizedOnlyPlan plan
    )
    {
        if (!data.ContainsKey(rooster.FarmAnimalName) || !data.ContainsKey(femaleKey))
            return;

        if (plan.UpgradeFemaleProduceToFertilized && !string.IsNullOrWhiteSpace(plan.FemaleFertilizedEggId))
        {
            data[femaleKey].DeluxeProduceItemIds = new List<FarmAnimalProduce> { new() { ItemId = plan.FemaleFertilizedEggId } };
            data[femaleKey].ProduceItemIds = new List<FarmAnimalProduce> { new() { ItemId = plan.FemaleFertilizedEggId } };
        }

        data[rooster.FarmAnimalName].EggItemIds = null;
        data[femaleKey].EggItemIds = null;
        foreach (var otherAnimal in otherAnimalsSharingEggs)
        {
            if (data.ContainsKey(otherAnimal))
                data[otherAnimal].EggItemIds = null;
        }

        if (plan.RoosterHatchesFertilizedEgg && !string.IsNullOrWhiteSpace(plan.RoosterFertilizedEggId))
        {
            data[rooster.FarmAnimalName].EggItemIds ??= new();
            data[rooster.FarmAnimalName].EggItemIds.Add(plan.RoosterFertilizedEggId);
            return;
        }

        if (!plan.RoosterHatchesFertilizedEgg && !string.IsNullOrWhiteSpace(plan.FemaleFertilizedEggId))
            data[femaleKey].EggItemIds = new List<string> { plan.FemaleFertilizedEggId };
    }

    public static List<string> GetAnimalsThatProduceRoosterEgg(
        IDictionary<string, FarmAnimalData> animalData,
        RoosterData rooster
    )
    {
        var roosterEggIds = rooster
            .Eggs.SelectMany(e => e.Value)
            .Where(e => e.Key == RoosterItemDataEntries.Name)
            .Select(e => e.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return animalData
            .Where(a => a.Value.EggItemIds is not null && a.Value.EggItemIds.Any(e => roosterEggIds.Contains(e)))
            .Select(a => a.Key)
            .ToList();
    }


    private static void RemoveEggItemIdIgnoreCase(List<string> eggItemIds, string eggItemIdToRemove)
    {
        if (eggItemIds is null || eggItemIds.Count == 0 || string.IsNullOrWhiteSpace(eggItemIdToRemove))
            return;

        for (var i = eggItemIds.Count - 1; i >= 0; i--)
        {
            if (string.Equals(eggItemIds[i], eggItemIdToRemove, StringComparison.OrdinalIgnoreCase))
                eggItemIds.RemoveAt(i);
        }
    }
}
