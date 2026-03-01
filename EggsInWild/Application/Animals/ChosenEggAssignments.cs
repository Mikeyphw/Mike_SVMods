using System;
using System.Collections.Generic;
using System.Linq;
using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData.FarmAnimals;
using StardewValley.GameData.Objects;
using Mikew.NestCore.Helpers.Logging;
using Mikew.NestCore;
using Mikew.NestCore.Framework;
using EggsInWild.Core.Enums;
using EggsInWild.Core.ShopAnchors;
using EggsInWild.Core.Weighting;
using EggsInWild.Infrastructure.ModData;
using EggsInWild.Infrastructure.ModData.Models;
using LogScopedHelper = Mikew.NestCore.Helpers.Logging.LogScopedHelper;
using static Mikew.NestCore.Helpers.Logging.ScopedLogCatalog;

namespace EggsInWild.Application.Animals;

/// <summary>
/// Computes a daily mapping from wild-egg item IDs to a farm-animal key.
/// Phase 5A: selection uses economy-derived weights (profitable animals = rarer),
/// and ignores any JSON-provided weights.
/// </summary>
public sealed class ChosenEggAssignments
{
    private static readonly StringComparer KeyComparer = StringComparer.OrdinalIgnoreCase;

    private readonly IModDataLoader _data;
    private readonly IModRegistry _registry;
    private readonly ILog _log;
    private readonly Func<string, bool> _isUnlocked;

    private readonly Dictionary<string, string> _eggToAnimal = new(KeyComparer);
    private int? _lastSeed;

    public ChosenEggAssignments(
        IModDataLoader data,
        IModRegistry registry,
        ILog log,
        Func<string, bool>? isUnlocked = null)
    {
        _data = data ?? throw new ArgumentNullException(nameof(data));
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _isUnlocked = isUnlocked ?? (_ => false);
    }

    public IReadOnlyDictionary<string, string> Current => _eggToAnimal;

    public void Clear()
    {
        _eggToAnimal.Clear();
        _lastSeed = null;
    }

    public void RecomputeIfNeeded(ProgressionStep step)
    {
        var farmAnimals = SafeLoad<Dictionary<string, FarmAnimalData>>(Strings.AssetNames.FarmAnimals);
        if (farmAnimals is null || farmAnimals.Count == 0)
        {
            LogScopedHelper.Log(_log, ScopedLogCatalog.ModWeCategory.Spawning, "ChosenEggAssignments: Data/FarmAnimals unavailable; skipping recompute.", LogLevel.Trace);
            return;
        }

        var anchors = ShopAnchorIndex.Build(farmAnimals, warn: msg => LogScopedHelper.Log(_log, ScopedLogCatalog.ModWeCategory.Spawning, msg, LogLevel.Trace));
        RecomputeIfNeeded(step, farmAnimals, anchors);
    }

    public void RecomputeIfNeeded(
        ProgressionStep step,
        IReadOnlyDictionary<string, FarmAnimalData> farmAnimals,
        ShopAnchorIndex anchors)
    {
        if (!ContextAvailable())
            return;

        if (farmAnimals is null)
            throw new ArgumentNullException(nameof(farmAnimals));

        anchors ??= ShopAnchorIndex.Empty;

        var daySeed = ComputeDaySeed();
        var seed = MixInt(MixInt(daySeed, (int)step), Game1.timeOfDay);

        if (_lastSeed == seed && _eggToAnimal.Count > 0)
            return;

        _eggToAnimal.Clear();

        var snapshot = _data.Load();

        var objects = SafeLoad<Dictionary<string, ObjectData>>(Strings.AssetNames.Objects);
        WeggEconomyWeightCalculator? economy = null;
        if (objects is not null && objects.Count > 0)
            economy = new WeggEconomyWeightCalculator(farmAnimals, anchors, objects, _isUnlocked);

        foreach (var eggId in SpawnAreaEggIds.GetEggItemIds(snapshot.SpawnAreas, _log))
        {
            var kind = EggKindExtensions.FromEggItemId(eggId);
            if (kind.IsMagic() && step < ProgressionStep.PartVI)
                continue;

            var regionToken = EggRegion.ExtractRegionToken(eggId);
            if (string.IsNullOrWhiteSpace(regionToken))
                continue;

            var eggSeed = MixInt(seed, StableHash32Upper(eggId));

            // Determine eligible wild-animal buckets (filtered to installed animals, then weighted by candidate key weights).
            var eligibleWild = snapshot.WildAnimals
                .Where(kvp => kvp.Value.Enabled && farmAnimals.ContainsKey(kvp.Key) && IsEligible(kvp.Value, regionToken, kind))
                .OrderBy(kvp => kvp.Key, KeyComparer)
                .ToList();

            if (eligibleWild.Count == 0)
                continue;

            var missingFarmAnimalKeys = new HashSet<string>(KeyComparer);
            var wildCandidates = new List<(string WildKey, WildAnimalJson Wild, List<string> Candidates, double Weight)>();

            foreach (var kvp in eligibleWild)
            {
                var wildKey = kvp.Key;
                var wild = kvp.Value;

                var candidates = BuildCandidateFarmAnimalIds(wild, kind, farmAnimals, out var missingKeys);
                foreach (var mk in missingKeys)
                    missingFarmAnimalKeys.Add(mk);

                if (candidates.Count == 0)
                    continue;

                var weight = ComputeWildWeight(candidates, economy);
                wildCandidates.Add((wildKey, wild, candidates, weight));
            }

            if (missingFarmAnimalKeys.Count > 0)
            {
                var orderedMissing = missingFarmAnimalKeys.OrderBy(k => k, KeyComparer).ToList();
                LogScopedHelper.Log(_log, ScopedLogCatalog.ModWeCategory.Spawning,
                    $"ChosenEggAssignments: egg={eggId} MissingFarmAnimalKeys=[{string.Join(", ", orderedMissing)}]",
                    LogLevel.Debug);
            }

            if (wildCandidates.Count == 0)
                continue;

            var orderedWild = wildCandidates.OrderBy(c => c.WildKey, KeyComparer).ToList();
            var weights = orderedWild.Select(c => c.Weight).ToList();

            if (!DeterministicWeightedPicker.TryPick(
                    orderedItems: orderedWild,
                    weights: weights,
                    seed: MixInt(eggSeed, 0x5B3A4D11),
                    out var chosenWild))
            {
                chosenWild = orderedWild[0];
            }

            if (!TryPickFarmAnimalKeyFromCandidates(
                    candidates: chosenWild.Candidates,
                    economy: economy,
                    seed: eggSeed,
                    out var farmAnimalKey))
            {
                continue;
            }

            _eggToAnimal[eggId] = farmAnimalKey;
        }

        _lastSeed = seed;
        LogScopedHelper.Log(_log, ScopedLogCatalog.ModWeCategory.Spawning, $"ChosenEggAssignments: mapped {_eggToAnimal.Count} eggs to farm animals for seed={seed}.", LogLevel.Debug);
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

    private static double ComputeWildWeight(
        IReadOnlyList<string> candidates,
        WeggEconomyWeightCalculator? economy)
    {
        if (candidates is null || candidates.Count == 0)
            return 0;

        if (economy is not null)
        {
            var weights = economy.ComputeWeights(candidates);
            if (weights.Count > 0)
            {
                var sum = weights.Values.Where(v => double.IsFinite(v) && v > 0).Sum();
                if (sum > 0)
                    return sum;
            }
        }

        return candidates.Count;
    }

    private bool TryPickFarmAnimalKeyFromCandidates(
        IReadOnlyList<string> candidates,
        WeggEconomyWeightCalculator? economy,
        int seed,
        out string farmAnimalKey)
    {
        farmAnimalKey = string.Empty;

        if (candidates is null || candidates.Count == 0)
            return false;

        if (economy is not null)
        {
            var weights = economy.ComputeWeights(candidates);
            if (weights.Count > 0
                && DeterministicWeightedPicker.TryPickKey(
                    weights,
                    seed: MixInt(seed, 0x2D81B1CF),
                    out var picked,
                    comparer: StringComparer.OrdinalIgnoreCase))
            {
                farmAnimalKey = picked;
                return !string.IsNullOrWhiteSpace(farmAnimalKey);
            }
        }

        // Fallback: deterministic uniform.
        var rng = new Random(MixInt(seed, unchecked((int)0x91E10DA5u)));
        farmAnimalKey = candidates[rng.Next(candidates.Count)];
        return true;
    }

    private bool TryPickFarmAnimalKey(
        WildAnimalJson wildAnimal,
        EggKind kind,
        IReadOnlyDictionary<string, FarmAnimalData> farmAnimals,
        WeggEconomyWeightCalculator? economy,
        int seed,
        out string farmAnimalKey)
    {
        var candidates = BuildCandidateFarmAnimalIds(wildAnimal, kind, farmAnimals, out _);
        return TryPickFarmAnimalKeyFromCandidates(candidates, economy, seed, out farmAnimalKey);
    }

    private List<string> BuildCandidateFarmAnimalIds(
        WildAnimalJson wildAnimal,
        EggKind kind,
        IReadOnlyDictionary<string, FarmAnimalData> farmAnimals,
        out IReadOnlyList<string> missingFarmAnimalKeys)
    {
        missingFarmAnimalKeys = Array.Empty<string>();
        if (wildAnimal.ExtraModAnimalKeys.Count == 0)
            return new List<string>();

        var available = wildAnimal.ExtraModAnimalKeys
            .Where(kvp => EggRegion.IsVanilla(kvp.Key) || _registry.IsLoaded(kvp.Key))
            .ToList();

        if (available.Count == 0)
            return new List<string>();

        // Prefer non-vanilla blocks if any are present.
        var nonVanilla = available.Where(kvp => !EggRegion.IsVanilla(kvp.Key)).ToList();
        var blocks = (nonVanilla.Count > 0 ? nonVanilla : available)
            .Select(kvp => kvp.Value)
            .ToList();

        var ids = new HashSet<string>(KeyComparer);
        foreach (var b in blocks)
        {
            foreach (var id in b.GetEffectiveFarmAnimalKeys())
                AddIfValid(id);
        }

        if (ids.Count == 0)
            return new List<string>();

        InjectLetMeHatchClones(ids);
        missingFarmAnimalKeys = ids
            .Where(id => !farmAnimals.ContainsKey(id))
            .OrderBy(id => id, KeyComparer)
            .ToList();

        var requiredHouse = kind.IsLarge() ? "Barn" : "Coop";

        var filtered = ids
            .Where(id => farmAnimals.TryGetValue(id, out var a) && a is not null && IsHouseCompatible(a, requiredHouse))
            .OrderBy(id => id, KeyComparer)
            .ToList();

        return filtered;

        void AddIfValid(string? id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return;

            ids.Add(id);
        }
    }

    private void InjectLetMeHatchClones(HashSet<string> ids)
    {
        if (ids is null || ids.Count == 0)
            return;

        try
        {
            var api = _registry.GetApi<ILetMeHatchApi>("Mikew.lmh");
            if (api is null)
                return;

            var pairs = api.GetClonedAnimalsInfo()?.ToList();
            if (pairs is null || pairs.Count == 0)
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
            LogScopedHelper.Log(_log, ScopedLogCatalog.ModWeCategory.Spawning, $"ChosenEggAssignments: LetMeHatch clone injection skipped ({ex.GetType().Name}: {ex.Message}).", LogLevel.Trace);
        }
    }



    private static bool IsHouseCompatible(FarmAnimalData animal, string requiredHouse)
    {
        var house = animal.House;
        if (string.IsNullOrWhiteSpace(house))
            return true;

        // Only enforce when the asset explicitly uses Coop/Barn houses.
        if (!house.Equals("Coop", StringComparison.OrdinalIgnoreCase)
            && !house.Equals("Barn", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return house.Equals(requiredHouse, StringComparison.OrdinalIgnoreCase);
    }

    private static bool ContextAvailable()
        => Game1.hasLoadedGame && Game1.player is not null;

    private static int ComputeDaySeed()
    {
        unchecked
        {
            var uid = (ulong)Game1.uniqueIDForThisGame;
            var days = (ulong)(int)Game1.stats.DaysPlayed;
            var x = uid ^ (days + 0x9E3779B97F4A7C15UL);
            x = SplitMix64(x);
            return (int)(x ^ (x >> 32));
        }
    }

    private static ulong SplitMix64(ulong x)
    {
        unchecked
        {
            x += 0x9E3779B97F4A7C15UL;
            x = (x ^ (x >> 30)) * 0xBF58476D1CE4E5B9UL;
            x = (x ^ (x >> 27)) * 0x94D049BB133111EBUL;
            return x ^ (x >> 31);
        }
    }

    private static int MixInt(int a, int b)
    {
        unchecked
        {
            uint x = (uint)a;
            uint y = (uint)b;
            uint z = x + 0x9E3779B9U + (y << 6) + (y >> 2);
            z ^= y + 0x85EBCA6BU + (z << 1);
            z ^= z >> 16;
            z *= 0x7FEB352DU;
            z ^= z >> 15;
            z *= 0x846CA68BU;
            z ^= z >> 16;
            return (int)z;
        }
    }

    private static int StableHash32Upper(string s)
    {
        unchecked
        {
            const uint fnvOffset = 2166136261;
            const uint fnvPrime = 16777619;

            var hash = fnvOffset;
            foreach (var c in s ?? string.Empty)
            {
                hash ^= (byte)char.ToUpperInvariant(c);
                hash *= fnvPrime;
            }

            return (int)hash;
        }
    }

    private static T? SafeLoad<T>(string assetName) where T : class
    {
        try { return Game1.content.Load<T>(assetName); }
        catch { return null; }
    }
}

/// <summary>Shared egg-ID parsing helpers (kept alongside chosen-animal logic for parity).</summary>
internal static class EggRegion
{
    public static bool IsVanilla(string modId)
        => string.Equals(modId, "Vanilla", StringComparison.OrdinalIgnoreCase);

    public static string ExtractRegionToken(string eggItemId)
    {
        const string prefix = "Mikew.eggsinwild_";

        if (string.IsNullOrWhiteSpace(eggItemId))
            return string.Empty;

        var token = eggItemId.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? eggItemId[prefix.Length..]
            : eggItemId;

        foreach (var suffix in new[] { "LMWEgg", "LWEgg", "MWEgg", "WEgg" })
        {
            if (token.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                return token[..^suffix.Length];
        }

        return token;
    }
}
