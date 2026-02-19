using System;
using System.Collections.Generic;
using System.Linq;
using StardewModdingAPI;
using StardewValley;
using Mikew.BirdsOfAFeather.Helpers.Logging;
using WildEggsHatching.Core.Enums;
using WildEggsHatching.Infrastructure.ModData;
using WildEggsHatching.Infrastructure.ModData.Models;

namespace WildEggsHatching.Application.Animals;

/// <summary>
/// Computes a daily mapping from wild-egg item IDs to a farm-animal key.
/// This mirrors the original "chosen animal" behavior but uses uniform weighting.
/// </summary>
public sealed class ChosenEggAssignments
{
    private readonly IModDataLoader _data;
    private readonly IModRegistry _registry;
    private readonly ILog _log;

    private readonly Dictionary<string, string> _eggToAnimal = new(StringComparer.OrdinalIgnoreCase);
    private int? _lastSeed;

    public ChosenEggAssignments(IModDataLoader data, IModRegistry registry, ILog log)
    {
        _data = data;
        _registry = registry;
        _log = log;
    }

    public IReadOnlyDictionary<string, string> Current => _eggToAnimal;

    public void Clear()
    {
        _eggToAnimal.Clear();
        _lastSeed = null;
    }

    public void RecomputeIfNeeded(ProgressionStep step)
    {
        if (!ContextAvailable())
            return;

        var seed = ComputeSeed();
        if (_lastSeed == seed && _eggToAnimal.Count > 0)
            return;

        _eggToAnimal.Clear();

        var snapshot = _data.Load();
        var rng = new Random(seed);

        foreach (var eggId in snapshot.SpawnAreas.Keys.OrderBy(s => s, StringComparer.OrdinalIgnoreCase))
        {
            var kind = EggKindExtensions.FromEggItemId(eggId);
            if (kind.IsMagic() && step < ProgressionStep.PartVI)
                continue;

            var regionToken = EggRegion.ExtractRegionToken(eggId);
            if (string.IsNullOrWhiteSpace(regionToken))
                continue;

            // Find wild-animal candidates that match size/magic and allow this region.
            var candidates = snapshot.WildAnimals
                .Where(kvp => IsEligible(kvp.Value, regionToken, kind))
                .ToList();

            if (candidates.Count == 0)
                continue;

            var chosen = candidates[rng.Next(candidates.Count)];
            if (!TryPickFarmAnimalKey(chosen.Value, rng, out var farmAnimalKey))
                continue;

            _eggToAnimal[eggId] = farmAnimalKey;
        }

        _lastSeed = seed;
        _log.Log($"ChosenEggAssignments: mapped {_eggToAnimal.Count} eggs to farm animals for day seed={seed}.", LogLevel.Debug);
    }

    private static bool IsEligible(WildAnimalJson wildAnimal, string regionToken, EggKind kind)
    {
        if (wildAnimal.IsMagic != kind.IsMagic())
            return false;

        if (wildAnimal.LargeEgg != kind.IsLarge())
            return false;

        // Region token is expected to match LocationTypes keys (e.g., Forest, IslandBeach).
        if (wildAnimal.LocationTypes.Count == 0)
            return true;

        return wildAnimal.LocationTypes.TryGetValue(regionToken, out var ok) && ok;
    }

    private bool TryPickFarmAnimalKey(WildAnimalJson wildAnimal, Random rng, out string farmAnimalKey)
    {
        farmAnimalKey = string.Empty;

        if (wildAnimal.ExtraModAnimalKeys.Count == 0)
            return false;

        // Prefer installed mods; fall back to Vanilla if present.
        var candidateBlocks = wildAnimal.ExtraModAnimalKeys
            .Where(kvp => EggRegion.IsVanilla(kvp.Key) || _registry.IsLoaded(kvp.Key))
            .OrderBy(kvp => EggRegion.IsVanilla(kvp.Key) ? 1 : 0)
            .Select(kvp => kvp.Value)
            .ToList();

        if (candidateBlocks.Count == 0)
            return false;

        var block = candidateBlocks[rng.Next(candidateBlocks.Count)];

        if (block.FarmAnimalKeysWeighted.Count > 0)
        {
            var total = block.FarmAnimalKeysWeighted.Sum(w => Math.Max(0, w.Weight));
            if (total <= 0)
                return false;

            var roll = rng.Next(total);
            foreach (var w in block.FarmAnimalKeysWeighted)
            {
                var weight = Math.Max(0, w.Weight);
                roll -= weight;
                if (roll < 0)
                {
                    farmAnimalKey = w.Id;
                    return !string.IsNullOrWhiteSpace(farmAnimalKey);
                }
            }

            return false;
        }

        if (block.FarmAnimalKeysSameWeight.Count == 0)
            return false;

        farmAnimalKey = block.FarmAnimalKeysSameWeight[rng.Next(block.FarmAnimalKeysSameWeight.Count)];
        return !string.IsNullOrWhiteSpace(farmAnimalKey);
    }

    private static bool ContextAvailable()
        => Game1.hasLoadedGame && Game1.player is not null;

    private static int ComputeSeed()
    {
        var uid = Game1.uniqueIDForThisGame;
        var days = (int)Game1.stats.DaysPlayed;
        return HashCode.Combine((int)(uid & 0xFFFFFFFF), (int)(uid >> 32), days, 0x5A17_6C2D);
    }
}

/// <summary>Shared egg-ID parsing helpers (kept alongside chosen-animal logic for parity).</summary>
internal static class EggRegion
{
    public static bool IsVanilla(string modId)
        => string.Equals(modId, "Vanilla", StringComparison.OrdinalIgnoreCase);

    public static string ExtractRegionToken(string eggItemId)
    {
        const string prefix = "Mikew.wildeggshatching_";

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
