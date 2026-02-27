using System;
using System.Collections.Generic;
using System.Linq;
using Mikew.NestCore.Helpers;
using StardewModdingAPI;
using StardewValley.GameData.FarmAnimals;

using static LetMeHatch.ModEntry;

namespace LetMeHatch.Core.Breeding;

/// <summary>
/// Applies <see cref="BreedingPlan"/> mutations to the farm animal game-data dictionary.
/// </summary>
public sealed class BreedingPlanExecutor : IBreedingPlanExecutor
{
    private readonly HashSet<string> _wildEggIds;

    public BreedingPlanExecutor(HashSet<string> wildEggIds)
    {
        _wildEggIds = wildEggIds ?? throw new ArgumentNullException(nameof(wildEggIds));
    }

    public void Apply(IDictionary<string, FarmAnimalData> farmAnimals, BreedingPlan plan)
    {
        if (farmAnimals is null) throw new ArgumentNullException(nameof(farmAnimals));
        if (plan is null) throw new ArgumentNullException(nameof(plan));

        foreach (var diag in plan.Diagnostics)
        {
            LoggerHelper.VerboseLog(diag, LogLevel.Trace);
        }

        try
        {
            foreach (var pair in plan.Pairs)
            {
                ApplyPair(farmAnimals, pair);
            }
        }
        finally
        {
            EnforceMaleNeverLaysInvariant(farmAnimals, plan);
        }
    }

    private void ApplyPair(IDictionary<string, FarmAnimalData> farmAnimals, BreedingPairPlan pair)
    {
        if (!farmAnimals.TryGetValue(pair.FemaleAnimalKey, out var femaleData))
        {
            LoggerHelper.VerboseLog($"BreedingPlan skipped: missing female key '{pair.FemaleAnimalKey}'.", LogLevel.Warn);
            return;
        }

        if (!farmAnimals.TryGetValue(pair.MaleAnimalKey, out var maleData))
        {
            LoggerHelper.VerboseLog($"BreedingPlan skipped: missing male key '{pair.MaleAnimalKey}'.", LogLevel.Warn);
            return;
        }

        if (string.IsNullOrEmpty(pair.EggToClearItemId))
        {
            return;
        }

        var sharers = GetAnimalsThatProduceSameEgg(farmAnimals, pair.EggToClearItemId);
        var otherMales = pair.OtherMaleKeys
            .Where(k => farmAnimals.TryGetValue(k, out _))
            .Select(k => (Key: k, Data: farmAnimals[k]))
            .ToList();

        ClearEggLists(pair.EggToClearItemId, femaleData, (pair.MaleAnimalKey, maleData), otherMales, sharers);

        if (pair.Mode == BreedingMode.Normal)
        {
            if (!string.IsNullOrEmpty(pair.EggToAssignItemId))
            {
                femaleData.EggItemIds ??= new List<string>();
                femaleData.EggItemIds.Add(pair.EggToAssignItemId);
            }

            return;
        }

        if (pair.Mode == BreedingMode.Fertilized && pair.FertilizedSucceeded && !string.IsNullOrEmpty(pair.EggToAssignItemId))
        {
            var eggId = pair.EggToAssignItemId;

            femaleData.DeluxeProduceItemIds ??= new List<FarmAnimalProduce>();
            femaleData.DeluxeProduceItemIds.Clear();
            femaleData.DeluxeProduceItemIds.Add(CreateFarmAnimalProduce(eggId));

            femaleData.ProduceItemIds ??= new List<FarmAnimalProduce>();
            femaleData.ProduceItemIds.Clear();
            femaleData.ProduceItemIds.Add(CreateFarmAnimalProduce(eggId));

            femaleData.EggItemIds ??= new List<string>();
            femaleData.EggItemIds.Add(eggId);
        }
    }

    private void ClearEggLists(
        string eggId,
        FarmAnimalData female,
        (string Key, FarmAnimalData Data) male,
        List<(string Key, FarmAnimalData Data)> otherMales,
        List<(string Key, FarmAnimalData Data)> animalsThatShareEgg
    )
    {
        if (IsMaleOnly(male.Key, male.Data))
        {
            male.Data.EggItemIds = null;
        }
        else
        {
            male.Data.EggItemIds ??= new List<string>();
            male.Data.EggItemIds.RemoveAll(item => !_wildEggIds.Contains(item));
        }

        female.EggItemIds ??= new List<string>();
        female.EggItemIds.RemoveAll(item => !_wildEggIds.Contains(item));

        foreach (var (key, data) in animalsThatShareEgg)
        {
            if (IsMaleOnly(key, data))
            {
                data.EggItemIds = null;
                continue;
            }

            data.EggItemIds ??= new List<string>();
            data.EggItemIds.Remove(eggId);
        }

        foreach (var (key, data) in otherMales)
        {
            if (IsMaleOnly(key, data))
            {
                data.EggItemIds = null;
                continue;
            }

            data.EggItemIds ??= new List<string>();
            data.EggItemIds.RemoveAll(item => !_wildEggIds.Contains(item));
        }
    }

    private static List<(string Key, FarmAnimalData Data)> GetAnimalsThatProduceSameEgg(
        IDictionary<string, FarmAnimalData> farmAnimals,
        string eggId
    )
    {
        return farmAnimals
            .Where(kvp =>
                kvp.Value.EggItemIds != null
                && kvp.Value.EggItemIds.Exists(e => string.Equals(e, eggId, StringComparison.OrdinalIgnoreCase))
            )
            .Select(kvp => (kvp.Key, kvp.Value))
            .ToList();
    }

    private static bool IsMaleOnly(FarmAnimalData data) => data.Gender == FarmAnimalGender.Male;

    private static bool IsMaleOnly(string animalKey, FarmAnimalData data)
    {
        if (IsMaleOnly(data))
        {
            return true;
        }

        return animalKey.EndsWith("#_Male", StringComparison.OrdinalIgnoreCase)
            || animalKey.EndsWith("_Male", StringComparison.OrdinalIgnoreCase);
    }

    private static FarmAnimalProduce CreateFarmAnimalProduce(string itemId)
        => new() { ItemId = itemId, Id = itemId };

    private static void EnforceMaleNeverLaysInvariant(IDictionary<string, FarmAnimalData> farmAnimals, BreedingPlan plan)
    {
        foreach (var mutation in plan.Mutations)
        {
            if (mutation is BreedingMutation.EnforceMaleNeverLays e && farmAnimals.TryGetValue(e.MaleKey, out var data))
            {
                if (IsMaleOnly(e.MaleKey, data))
                {
                    data.EggItemIds = null;
                }
            }
        }
    }
}
