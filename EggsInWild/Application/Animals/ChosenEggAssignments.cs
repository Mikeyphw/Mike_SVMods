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

        var catalog = WildEggSpawnCatalogBuilder.Build(snapshot.WildEggSpawnDefinitions, snapshot.WildAnimals, farmAnimals, _registry, _log);

        foreach (var definition in catalog.ActiveEggDefinitions)
        {
            var eggId = definition.EggItemId;
            var kind = definition.Kind;
            if (kind.IsMagic() && step < ProgressionStep.PartVI)
                continue;

            var eggSeed = MixInt(seed, StableHash32Upper(eggId));
            var missingFarmAnimalKeys = definition.EligibleWildAnimals
                .SelectMany(bucket => bucket.MissingFarmAnimalKeys)
                .Distinct(KeyComparer)
                .OrderBy(key => key, KeyComparer)
                .ToList();

            if (missingFarmAnimalKeys.Count > 0)
            {
                LogScopedHelper.Log(_log, ScopedLogCatalog.ModWeCategory.Spawning,
                    $"ChosenEggAssignments: egg={eggId} MissingFarmAnimalKeys=[{string.Join(", ", missingFarmAnimalKeys)}]",
                    LogLevel.Debug);
            }

            if (definition.EligibleWildAnimals.Count == 0)
                continue;

            var orderedWild = definition.EligibleWildAnimals
                .Select(bucket => new
                {
                    bucket.WildAnimalKey,
                    Candidates = bucket.CandidateFarmAnimalKeys,
                    Weight = ComputeWildWeight(bucket.CandidateFarmAnimalKeys, economy),
                })
                .OrderBy(bucket => bucket.WildAnimalKey, KeyComparer)
                .ToList();

            var weights = orderedWild.Select(bucket => bucket.Weight).ToList();

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
