using System;
using System.Collections.Generic;
using Mikew.BirdsOfAFeather.Framework.Random;
using StardewModdingAPI;
using StardewValley;
using WildEggsHatching.Application.Spawning.Diagnostics;
using WildEggsHatching.Core.Enums;
using WildEggsHatching.Infrastructure.Game;
using Mikew.BirdsOfAFeather.Helpers.Logging;
using WildEggsHatching.Infrastructure.ModData.Models;

namespace WildEggsHatching.Application.Spawning;

public sealed class EggSpawnService
{
    private const string Vanilla = "Vanilla";

    private readonly IWorldLocations _world;
    private readonly IGameObjectPlacer _placer;
    private readonly ITileFinder _tileFinder;
    private readonly ILog _log;
    private readonly ITileSearchHistogram _histogram;

    public EggSpawnService(
        IWorldLocations world,
        IGameObjectPlacer placer,
        ITileFinder tileFinder,
        ILog log,
        ITileSearchHistogram? histogram = null)
    {
        _world = world;
        _placer = placer;
        _tileFinder = tileFinder;
        _log = log;
        _histogram = histogram ?? new TileSearchHistogram();
    }

    public void SpawnForToday(SpawnPlan plan, SpawnParametersJson parameters, long saveId, int dayNumber)
    {
        var budgets = new Dictionary<string, int>(plan.PackEggBudgets, StringComparer.OrdinalIgnoreCase);

        // Map caps include eggs already present in the world (legacy semantics).
        var worldLocations = _world.GetAll();
        var mapCaps = SpawnLimits.SnapshotFromWorld(worldLocations);

        // World caps include eggs already present in the world (stricter legacy semantics).
        var mapIsVanilla = BuildMapIsVanilla(plan);
        var (spawnedWorldVanilla, spawnedWorldPacks) = CountExistingWorldEggs(worldLocations, mapIsVanilla);

        var maxWorldVanilla = DetermineWorldBudget(parameters.Parameters_Vanilla, saveId, dayNumber, group: "vanilla-world");
        var maxWorldPacks = DetermineWorldBudget(parameters.Parameters_Packs, saveId, dayNumber, group: "packs-world");

        var spawnedByKind = CountExistingWorldEggsByKind(worldLocations);

        foreach (var job in plan.Jobs)
        {
            if (!budgets.TryGetValue(job.PackId, out var remaining) || remaining <= 0)
                continue;

            var isVanilla = string.Equals(job.PackId, Vanilla, StringComparison.OrdinalIgnoreCase);
            if (isVanilla && spawnedWorldVanilla >= maxWorldVanilla)
            {
                _log.Log($"Spawn skipped: world vanilla cap hit ({spawnedWorldVanilla}/{maxWorldVanilla}) (map={job.MapName})", LogLevel.Trace);
                continue;
            }

            if (!isVanilla && spawnedWorldPacks >= maxWorldPacks)
            {
                _log.Log($"Spawn skipped: world packs cap hit ({spawnedWorldPacks}/{maxWorldPacks}) (map={job.MapName})", LogLevel.Trace);
                continue;
            }

            var location = _world.TryGetByName(job.MapName);
            if (location is null)
                continue;

            var spawnedInJob = 0;

            for (var i = 0; i < job.EggsToSpawn; i++)
            {
                if (budgets[job.PackId] <= 0)
                    break;

                if (isVanilla && spawnedWorldVanilla >= maxWorldVanilla)
                    break;

                if (!isVanilla && spawnedWorldPacks >= maxWorldPacks)
                    break;

                var kindCap = SpawnLimits.GetPerKindWorldCap(job.EggKind, parameters);
                var kindCount = spawnedByKind.TryGetValue(job.EggKind, out var kc) ? kc : 0;
                if (kindCount >= kindCap)
                {
                    _log.Log($"Spawn skipped: per-kind world cap hit ({job.EggKind}:{kindCount}/{kindCap}) (job={job.EggItemId} map={job.MapName})", LogLevel.Trace);
                    break;
                }

                if (!mapCaps.CanSpawnInMap(job.MapName, job.EggKind, parameters, out var capReason))
                {
                    _log.Log($"Spawn skipped: {capReason} (job={job.EggItemId} map={job.MapName})", LogLevel.Trace);
                    break;
                }

                var rng = new SeededRandomProvider(DeterministicSeed.For(saveId, dayNumber, "spawn", job.EggItemId, job.MapName, i, spawnedInJob));

                var tile = _tileFinder.FindTile(location, job, rng, parameters.MaxSpawningTries, out var stats);
                _histogram.Record(job.MapName, job.EggItemId, stats, foundTile: tile is not null);

                if (tile is null)
                {
                    var top = _histogram.FormatTopRejections(job.MapName, job.EggItemId, topN: 3);
                    _log.Log(
                        $"No valid tile (job={job.EggItemId} map={job.MapName}) " +
                        $"attempts={stats.Attempts} outOfBounds={stats.RejectedOutOfBounds} excluded={stats.RejectedExcludedRect} terrain={stats.RejectedTerrain} water={stats.RejectedWater} " +
                        $"occupied={stats.RejectedOccupied} impassable={stats.RejectedImpassable} top=[{top}]",
                        LogLevel.Trace);
                    break;
                }

                if (!_placer.TryPlaceObject(location, tile.Value, job.EggItemId, 1))
                {
                    _log.Log($"Failed placing egg object (job={job.EggItemId} map={job.MapName} tile={tile})", LogLevel.Warn);
                    continue;
                }

                budgets[job.PackId]--;
                mapCaps.RecordSpawn(job.MapName, job.EggKind);

                spawnedByKind[job.EggKind] = kindCount + 1;

                if (isVanilla)
                    spawnedWorldVanilla++;
                else
                    spawnedWorldPacks++;

                spawnedInJob++;
            }

            if (spawnedInJob > 0)
                _log.Log($"Spawned {spawnedInJob} eggs for {job.EggItemId} in {job.MapName} (pack={job.PackId}).", LogLevel.Debug);
        }
    }

    public void DumpEndOfDayTileSearchFailureSummary()
    {
        var top = _histogram.FormatTopRejectionsAcrossFailures(topN: 3);
        if (!string.IsNullOrWhiteSpace(top))
            _log.Log($"Tile search failures summary (end-of-day): top=[{top}]", LogLevel.Trace);

        _histogram.Reset();
    }

    
    private static Dictionary<string, bool> BuildMapIsVanilla(SpawnPlan plan)
    {
        var mapIsVanilla = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        foreach (var job in plan.Jobs)
        {
            if (string.IsNullOrWhiteSpace(job.MapName))
                continue;

            var isVanilla = string.Equals(job.PackId, Vanilla, StringComparison.OrdinalIgnoreCase);

            if (mapIsVanilla.TryGetValue(job.MapName, out var existing))
            {
                if (existing != isVanilla)
                    mapIsVanilla[job.MapName] = existing; // Prefer first-seen; conflicting config should be fixed at source.
                continue;
            }

            mapIsVanilla[job.MapName] = isVanilla;
        }

        return mapIsVanilla;
    }

    private static (int vanilla, int packs) CountExistingWorldEggs(
        IEnumerable<GameLocation> locations,
        IReadOnlyDictionary<string, bool> mapIsVanilla)
    {
        var vanilla = 0;
        var packs = 0;

        foreach (var loc in locations)
        {
            if (loc is null)
                continue;

            var locName = loc.NameOrUniqueName;

            foreach (var pair in loc.objects.Pairs)
            {
                if (pair.Value is not StardewValley.Object obj)
                    continue;

                if (!IsWildEgg(obj.ItemId))
                    continue;

                var isVanilla = mapIsVanilla.TryGetValue(locName, out var v) ? v : true;
                if (isVanilla)
                    vanilla++;
                else
                    packs++;
            }
        }

        return (vanilla, packs);
    }


    private static Dictionary<EggKind, int> CountExistingWorldEggsByKind(IEnumerable<GameLocation> locations)
    {
        var counts = new Dictionary<EggKind, int>();

        foreach (var loc in locations)
        {
            if (loc is null)
                continue;

            foreach (var pair in loc.objects.Pairs)
            {
                if (pair.Value is not StardewValley.Object obj)
                    continue;

                if (!IsWildEgg(obj.ItemId))
                    continue;

                var kind = EggKindExtensions.FromEggItemId(obj.ItemId);
                counts[kind] = counts.TryGetValue(kind, out var c) ? c + 1 : 1;
            }
        }

        return counts;
    }

    private static bool IsWildEgg(string itemId)
        => itemId.StartsWith("Mikew.wildeggshatching_", StringComparison.OrdinalIgnoreCase)
           && itemId.EndsWith("Egg", StringComparison.OrdinalIgnoreCase);


private static int DetermineWorldBudget(ParameterPackJson packParams, long saveId, int dayNumber, string group)
    {
        var min = Math.Max(0, packParams.MinEggs);
        var max = Math.Max(min, packParams.MaxEggs_All);

        var rng = new SeededRandomProvider(DeterministicSeed.For(saveId, dayNumber, group));
        return rng.Next(min, max + 1);
    }
}
