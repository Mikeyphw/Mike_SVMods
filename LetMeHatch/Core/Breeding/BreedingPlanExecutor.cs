using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Mikew.NestCore.Helpers;
using StardewModdingAPI;
using StardewValley.GameData.FarmAnimals;

using static LetMeHatch.ModEntry;
using static Mikew.NestCore.EnumsAndStrings.Strings.ItemIds;

namespace LetMeHatch.Core.Breeding;

/// <summary>
/// Applies <see cref="BreedingPlan"/> mutations to the farm animal game-data dictionary.
/// </summary>
public sealed class BreedingPlanExecutor : IBreedingPlanExecutor
{
    private const string ManagedEggItemIdsKey = "LetMeHatch.ManagedEggItemIds";
    private const string ManagedByLetMeHatchKey = "LetMeHatch.ManagedByLetMeHatch";
    private const string EggIdPattern = @".*egg.*";
    private const char ManagedEggDelimiter = '|';

    private readonly HashSet<string> _wildEggIds;

    public BreedingPlanExecutor(HashSet<string> wildEggIds)
    {
        _wildEggIds = wildEggIds ?? throw new ArgumentNullException(nameof(wildEggIds));
    }

    public void Apply(IDictionary<string, FarmAnimalData> farmAnimals, BreedingPlan plan)
    {
        if (farmAnimals is null) throw new ArgumentNullException(nameof(farmAnimals));
        if (plan is null) throw new ArgumentNullException(nameof(plan));

        var scope = new HashSet<string>(plan.ScopeAnimalKeys ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);

        foreach (var diag in plan.Diagnostics)
        {
            LoggerHelper.VerboseLog(diag, LogLevel.Trace);
        }

        try
        {
            foreach (var pair in plan.Pairs)
            {
                ApplyPair(farmAnimals, scope, pair);
            }
        }
        finally
        {
            EnforceMaleNeverLaysInvariant(farmAnimals, plan);
        }
    }

    private void ApplyPair(IDictionary<string, FarmAnimalData> farmAnimals, HashSet<string> scope, BreedingPairPlan pair)
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

        var eggId = pair.EggToClearItemId;
        var sharers = GetAnimalsThatProduceSameEgg(farmAnimals, scope, eggId);

        var otherMales = pair.OtherMaleKeys
            .Where(k => scope.Contains(k) && farmAnimals.TryGetValue(k, out _))
            .Select(k => (Key: k, Data: farmAnimals[k]))
            .ToList();

        LoggerHelper.VerboseLog(
            $"BreedingPlan apply: female='{pair.FemaleAnimalKey}' male='{pair.MaleAnimalKey}' mode={pair.Mode} egg='{eggId}' sharers={sharers.Count}.",
            LogLevel.Trace);

        var before = SnapshotEggDomain(femaleData);

        ClearEggDomain(scope, eggId, femaleData, (pair.MaleAnimalKey, maleData), otherMales, sharers);

        if (pair.Mode == BreedingMode.Normal)
        {
            if (!string.IsNullOrEmpty(pair.EggToAssignItemId))
            {
                AssignEggToFemale(femaleData, pair.EggToAssignItemId);
            }

            LoggerHelper.VerboseLog($"BreedingPlan egg-domain diff: {before} => {SnapshotEggDomain(femaleData)}.", LogLevel.Trace);
            return;
        }

        if (pair.Mode == BreedingMode.Fertilized && pair.FertilizedSucceeded && !string.IsNullOrEmpty(pair.EggToAssignItemId))
        {
            AssignFertilizedEggToFemale(femaleData, pair.EggToAssignItemId);
        }

        LoggerHelper.VerboseLog($"BreedingPlan egg-domain diff: {before} => {SnapshotEggDomain(femaleData)}.", LogLevel.Trace);
    }

    private void ClearEggDomain(
        HashSet<string> scope,
        string eggId,
        FarmAnimalData female,
        (string Key, FarmAnimalData Data) male,
        List<(string Key, FarmAnimalData Data)> otherMales,
        List<(string Key, FarmAnimalData Data)> animalsThatShareEgg
    )
    {
        if (scope.Contains(male.Key))
        {
            ClearEggDomainForKey(male.Key, male.Data, eggId);
        }

        ClearEggDomainForKey(null, female, eggId);

        foreach (var (key, data) in animalsThatShareEgg)
        {
            if (scope.Contains(key))
            {
                ClearEggDomainForKey(key, data, eggId);
            }
        }

        foreach (var (key, data) in otherMales)
        {
            ClearEggDomainForKey(key, data, eggId);
        }
    }

    private void ClearEggDomainForKey(string? animalKey, FarmAnimalData data, string eggId)
    {
        if (animalKey != null && IsMaleOnly(animalKey, data))
        {
            data.EggItemIds = null;
            MarkManaged(data, managedEggIds: null);
            return;
        }

        var managed = ReadManagedEggIds(data);
        managed.RemoveWhere(id => _wildEggIds.Contains(id));

        data.EggItemIds ??= new List<string>();
        data.EggItemIds.RemoveAll(id =>
            (!IsWildEgg(id) && managed.Contains(id))
            || (!IsWildEgg(eggId) && string.Equals(id, eggId, StringComparison.OrdinalIgnoreCase)));

        // Egg-domain-only: strip egg-like produces but preserve non-egg produces.
        StripEggProduces(data, explicitEggIdToRemove: eggId);

        managed.RemoveWhere(id => string.Equals(id, eggId, StringComparison.OrdinalIgnoreCase));
        WriteManagedEggIds(data, managed);
    }

    private void AssignEggToFemale(FarmAnimalData femaleData, string eggId)
    {
        femaleData.EggItemIds ??= new List<string>();
        if (!femaleData.EggItemIds.Contains(eggId, StringComparer.OrdinalIgnoreCase))
        {
            femaleData.EggItemIds.Add(eggId);
        }

        if (IsWildEgg(eggId))
        {
            MarkManaged(femaleData, ReadManagedEggIds(femaleData));
            return;
        }

        var managed = ReadManagedEggIds(femaleData);
        managed.Add(eggId);
        WriteManagedEggIds(femaleData, managed);
    }

    private void AssignFertilizedEggToFemale(FarmAnimalData femaleData, string eggId)
    {
        AssignEggToFemale(femaleData, eggId);

        // Preserve non-egg produce, but ensure fertilized egg is present.
        femaleData.ProduceItemIds ??= new List<FarmAnimalProduce>();
        femaleData.DeluxeProduceItemIds ??= new List<FarmAnimalProduce>();

        StripEggProduces(femaleData, explicitEggIdToRemove: null);

        UpsertProduce(femaleData.ProduceItemIds, eggId);
        UpsertProduce(femaleData.DeluxeProduceItemIds, eggId);
    }

    private static void UpsertProduce(IList<FarmAnimalProduce> list, string eggId)
    {
        if (list.Any(p => string.Equals(p.ItemId, eggId, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        list.Add(new FarmAnimalProduce { ItemId = eggId, Id = eggId });
    }

    private void StripEggProduces(FarmAnimalData data, string? explicitEggIdToRemove)
    {
        if (data.ProduceItemIds != null)
        {
            data.ProduceItemIds.RemoveAll(p => ShouldRemoveProduce(p.ItemId, explicitEggIdToRemove));
        }

        if (data.DeluxeProduceItemIds != null)
        {
            data.DeluxeProduceItemIds.RemoveAll(p => ShouldRemoveProduce(p.ItemId, explicitEggIdToRemove));
        }
    }

    private bool ShouldRemoveProduce(string? itemId, string? explicitEggIdToRemove)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            return false;
        }

        if (explicitEggIdToRemove != null
            && !IsWildEgg(explicitEggIdToRemove)
            && string.Equals(itemId, explicitEggIdToRemove, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (IsWildEgg(itemId))
        {
            return false;
        }

        return IsEggLikeId(itemId);
    }

    private static List<(string Key, FarmAnimalData Data)> GetAnimalsThatProduceSameEgg(
        IDictionary<string, FarmAnimalData> farmAnimals,
        HashSet<string> scope,
        string eggId
    )
    {
        if (string.IsNullOrWhiteSpace(eggId))
        {
            return new List<(string, FarmAnimalData)>();
        }

        return farmAnimals
            .Where(kvp =>
                scope.Contains(kvp.Key)
                && kvp.Value.EggItemIds != null
                && kvp.Value.EggItemIds.Exists(e => string.Equals(e, eggId, StringComparison.OrdinalIgnoreCase))
            )
            .Select(kvp => (kvp.Key, kvp.Value))
            .ToList();
    }

    private bool IsWildEgg(string itemId) => _wildEggIds.Contains(itemId);

    private static bool IsEggLikeId(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            return false;
        }

        if (AnimalProducts.VanillaEggs.Contains(itemId))
        {
            return true;
        }

        return Regex.IsMatch(itemId, EggIdPattern, RegexOptions.IgnoreCase);
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

    private static HashSet<string> ReadManagedEggIds(FarmAnimalData data)
    {
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (data.CustomFields is null)
        {
            return ids;
        }

        if (!data.CustomFields.TryGetValue(ManagedEggItemIdsKey, out var raw) || string.IsNullOrWhiteSpace(raw))
        {
            return ids;
        }

        foreach (var part in raw.Split(ManagedEggDelimiter, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!string.IsNullOrWhiteSpace(part))
            {
                ids.Add(part);
            }
        }

        return ids;
    }

    private static void WriteManagedEggIds(FarmAnimalData data, HashSet<string> managedEggIds)
    {
        data.CustomFields ??= new Dictionary<string, string>();

        if (managedEggIds.Count == 0)
        {
            data.CustomFields.Remove(ManagedEggItemIdsKey);
        }
        else
        {
            data.CustomFields[ManagedEggItemIdsKey] = string.Join(
                ManagedEggDelimiter,
                managedEggIds.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            );
        }

        data.CustomFields[ManagedByLetMeHatchKey] = "true";
    }

    private static void MarkManaged(FarmAnimalData data, HashSet<string>? managedEggIds)
    {
        data.CustomFields ??= new Dictionary<string, string>();
        data.CustomFields[ManagedByLetMeHatchKey] = "true";

        if (managedEggIds is null || managedEggIds.Count == 0)
        {
            data.CustomFields.Remove(ManagedEggItemIdsKey);
            return;
        }

        data.CustomFields[ManagedEggItemIdsKey] = string.Join(
            ManagedEggDelimiter,
            managedEggIds.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
        );
    }

    private static string SnapshotEggDomain(FarmAnimalData data)
    {
        var eggCount = data.EggItemIds?.Count ?? 0;
        var prodCount = data.ProduceItemIds?.Count ?? 0;
        var deluxeCount = data.DeluxeProduceItemIds?.Count ?? 0;

        var managed = data.CustomFields != null && data.CustomFields.TryGetValue(ManagedEggItemIdsKey, out var m) ? m : "";
        if (managed.Length > 120)
        {
            managed = managed[..120] + "...";
        }

        return $"eggs={eggCount} produce={prodCount} deluxe={deluxeCount} managed='{managed}'";
    }

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
