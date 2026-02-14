using System;
using System.Collections.Generic;
using Mikew.BirdsOfAFeather.Framework.Random;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using WildEggsHatching.Infrastructure.Game;
using WildEggsHatching.Infrastructure.Logging;
using WildEggsHatching.Infrastructure.ModData.Models;

namespace WildEggsHatching.Application.Spawning;

public sealed class EggSpawnService
{
    private readonly IWorldLocations _world;
    private readonly IGameObjectPlacer _placer;
    private readonly ITileFinder _tileFinder;
    private readonly ILog _log;

    public EggSpawnService(IWorldLocations world, IGameObjectPlacer placer, ITileFinder tileFinder, ILog log)
    {
        _world = world;
        _placer = placer;
        _tileFinder = tileFinder;
        _log = log;
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
                if (tile is null)
                {
                    _log.Log(
                        $"No valid tile (job={job.EggItemId} map={job.MapName}) attempts={stats.Attempts} excluded={stats.RejectedExcludedRect} terrain={stats.RejectedTerrain} water={stats.RejectedWater} notPlaceable={stats.RejectedNotPlaceable}",
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
}
