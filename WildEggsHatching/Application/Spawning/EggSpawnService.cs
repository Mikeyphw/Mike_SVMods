using System;
using System.Collections.Generic;
using Mikew.BirdsOfAFeather.Framework.Random;
using StardewModdingAPI;
using StardewValley;
using WildEggsHatching.Application.Spawning.Diagnostics;
using WildEggsHatching.Infrastructure.Game;
using Mikew.BirdsOfAFeather.Helpers.Logging;
using WildEggsHatching.Infrastructure.ModData.Models;

namespace WildEggsHatching.Application.Spawning;

public sealed class EggSpawnService
{
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
        var limits = SpawnLimits.SnapshotFromWorld(_world.GetAll());

        foreach (var job in plan.Jobs)
        {
            if (!budgets.TryGetValue(job.PackId, out var remaining) || remaining <= 0)
                continue;

            var location = _world.TryGetByName(job.MapName);
            if (location is null)
                continue;

            var spawnedInJob = 0;

            for (var i = 0; i < job.EggsToSpawn; i++)
            {
                if (budgets[job.PackId] <= 0)
                    break;

                if (!limits.CanSpawnInMap(job.MapName, job.EggKind, parameters, out var capReason))
                {
                    _log.Log($"Spawn skipped: {capReason} (job={job.EggItemId} map={job.MapName})", LogLevel.Trace);
                    break;
                }

                var rng = new SeededRandomProvider(DeterministicSeed.For(saveId, dayNumber, "spawn", job.EggItemId, job.MapName, i, spawnedInJob));

                if (!job.BypassRandomChance && rng.NextDouble() >= job.SpawnChance)
                    continue;

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
                limits.RecordSpawn(job.MapName, job.EggKind);
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
}
