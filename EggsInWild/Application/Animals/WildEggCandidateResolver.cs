using System;
using System.Collections.Generic;
using System.Linq;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley.GameData.FarmAnimals;
using EggsInWild.Core.Enums;
using EggsInWild.Infrastructure.ModData;
using EggsInWild.Infrastructure.ModData.Models;
using Mikew.NestCore.Framework;
using Mikew.NestCore.Helpers.Logging;
using LogScopedHelper = Mikew.NestCore.Helpers.Logging.LogScopedHelper;
using static Mikew.NestCore.Helpers.Logging.ScopedLogCatalog;

namespace EggsInWild.Application.Animals;

internal static class WildEggCandidateResolver
{
    private static readonly StringComparer KeyComparer = StringComparer.OrdinalIgnoreCase;

    public static IReadOnlyList<WildEggAnimalCandidateBucket> GetEligibleWildCandidates(
        string locationToken,
        EggKind kind,
        IReadOnlyDictionary<string, WildAnimalJson> wildAnimals,
        IReadOnlyDictionary<string, FarmAnimalData> farmAnimals,
        IModRegistry registry,
        ILog log)
    {
        var eligibleWild = wildAnimals
            .Where(kvp => kvp.Value.Enabled && farmAnimals.ContainsKey(kvp.Key) && IsEligible(kvp.Value, locationToken, kind))
            .OrderBy(kvp => kvp.Key, KeyComparer)
            .ToList();

        var buckets = new List<WildEggAnimalCandidateBucket>(eligibleWild.Count);
        foreach (var kvp in eligibleWild)
        {
            var candidates = BuildCandidateFarmAnimalIds(kvp.Value, kind, farmAnimals, registry, log, out var missingKeys);
            if (candidates.Count == 0)
                continue;

            buckets.Add(new WildEggAnimalCandidateBucket(
                WildAnimalKey: kvp.Key,
                CandidateFarmAnimalKeys: candidates,
                MissingFarmAnimalKeys: missingKeys));
        }

        return buckets;
    }

    private static bool IsEligible(WildAnimalJson wildAnimal, string regionToken, EggKind kind)
    {
        if (wildAnimal.IsMagic != kind.IsMagic())
            return false;

        if (wildAnimal.LargeEgg != kind.IsLarge())
            return false;

        if (wildAnimal.LocationTypes.Count == 0)
            return true;

        return wildAnimal.LocationTypes.TryGetValue(regionToken, out var ok) && ok;
    }

    private static IReadOnlyList<string> BuildCandidateFarmAnimalIds(
        WildAnimalJson wildAnimal,
        EggKind kind,
        IReadOnlyDictionary<string, FarmAnimalData> farmAnimals,
        IModRegistry registry,
        ILog log,
        out IReadOnlyList<string> missingFarmAnimalKeys)
    {
        missingFarmAnimalKeys = Array.Empty<string>();
        if (wildAnimal.ExtraModAnimalKeys.Count == 0)
            return Array.Empty<string>();

        var available = wildAnimal.ExtraModAnimalKeys
            .Where(kvp => EggRegion.IsVanilla(kvp.Key) || registry.IsLoaded(kvp.Key))
            .ToList();

        if (available.Count == 0)
            return Array.Empty<string>();

        var nonVanilla = available.Where(kvp => !EggRegion.IsVanilla(kvp.Key)).ToList();
        var blocks = (nonVanilla.Count > 0 ? nonVanilla : available)
            .Select(kvp => kvp.Value)
            .ToList();

        var ids = new HashSet<string>(KeyComparer);
        foreach (var block in blocks)
        {
            foreach (var id in block.GetEffectiveFarmAnimalKeys())
            {
                if (!string.IsNullOrWhiteSpace(id))
                    ids.Add(id);
            }
        }

        if (ids.Count == 0)
            return Array.Empty<string>();

        InjectLetMeHatchClones(ids, registry, log);

        missingFarmAnimalKeys = ids
            .Where(id => !farmAnimals.ContainsKey(id))
            .OrderBy(id => id, KeyComparer)
            .ToList();

        var requiredHouse = kind.IsLarge() ? "Barn" : "Coop";

        return ids
            .Where(id => farmAnimals.TryGetValue(id, out var animal) && animal is not null && IsHouseCompatible(animal, requiredHouse))
            .OrderBy(id => id, KeyComparer)
            .ToList();
    }

    private static void InjectLetMeHatchClones(HashSet<string> ids, IModRegistry registry, ILog log)
    {
        if (ids.Count == 0)
            return;

        if (!Context.IsGameLaunched)
            return;

        try
        {
            var api = registry.GetApi<ILetMeHatchApi>("Mikew.lmh");
            if (api is null || !api.FinishedEarlyStuff)
                return;

            var pairs = api.GetClonedAnimalsInfo()?.ToList();
            if (pairs is null || !pairs.Any())
                return;

            foreach (var pair in pairs)
            {
                var female = pair.FemaleKey;
                var male = pair.MaleKey;
                if (string.IsNullOrWhiteSpace(female) || string.IsNullOrWhiteSpace(male))
                    continue;
                if (female.Equals(male, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (ids.Contains(female))
                    ids.Add(male);
            }
        }
        catch (Exception ex)
        {
            LogScopedHelper.Log(log, ScopedLogCatalog.ModWeCategory.Spawning, $"WildEgg candidate clone injection skipped ({ex.GetType().Name}: {ex.Message}).", StardewModdingAPI.LogLevel.Trace);
        }
    }

    private static bool IsHouseCompatible(FarmAnimalData animal, string requiredHouse)
    {
        var house = animal.House;
        if (string.IsNullOrWhiteSpace(house))
            return true;

        if (!house.Equals("Coop", StringComparison.OrdinalIgnoreCase)
            && !house.Equals("Barn", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return house.Equals(requiredHouse, StringComparison.OrdinalIgnoreCase);
    }
}
