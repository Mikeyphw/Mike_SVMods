using System;
using System.Collections.Generic;
using System.Linq;
using Mikew.BirdsOfAFeather.Framework.Random;
using WildEggsHatching.Core.Enums;
using WildEggsHatching.Infrastructure.Game;
using Mikew.BirdsOfAFeather.Helpers.Logging;
using WildEggsHatching.Infrastructure.ModData;
using WildEggsHatching.Infrastructure.ModData.Models;
using WildEggsHatching.Infrastructure.Mods;

namespace WildEggsHatching.Application.Spawning;

public sealed class SpawnPlanner
{
    private const string Vanilla = "Vanilla";

    private readonly IWorldLocations _world;
    private readonly IModAvailability _mods;
    private readonly IAreaConditionsEvaluator _conditions;
    private readonly ILog _log;

    public SpawnPlanner(
        IWorldLocations world,
        IModAvailability mods,
        IAreaConditionsEvaluator conditions,
        ILog log)
    {
        _world = world;
        _mods = mods;
        _conditions = conditions;
        _log = log;
    }

    public SpawnPlan BuildPlan(ModDataSnapshot snapshot, long saveId, int dayNumber)
    {
        var parameters = snapshot.SpawnParameters;
        var chanceCalc = new SpawnChanceCalculator(parameters);

        var activePacks = snapshot.LocationPacks
            .Where(p => _mods.IsLoaded(p.Key))
            .ToDictionary(x => x.Key, y => y.Value, StringComparer.OrdinalIgnoreCase);

        var packBudgets = BuildPackBudgets(activePacks, parameters, saveId, dayNumber);

        var jobsWithKey = new List<(double key, SpawnJob job)>();

        foreach (var (eggItemId, rules) in SpawnAreaEggIds.Normalize(snapshot.SpawnAreas, _log))
        {
            var kind = EggKindExtensions.FromEggItemId(eggItemId);

            foreach (var rule in rules)
            {
                if (!activePacks.TryGetValue(rule.ModID, out var pack))
                    continue;

                if (IsExcludedLocation(rule.MapName))
                    continue;

                if (!_world.Exists(rule.MapName))
                    continue;

                if (!PackContains(pack, rule.Location, rule.MapName))
                    continue;

                if (!_conditions.AreMet(rule.ExtraConditions))
                    continue;

                var packParams = IsVanilla(rule.ModID) ? parameters.Parameters_Vanilla : parameters.Parameters_Packs;

                var eggsToSpawn = DetermineEggsToSpawn(rule, packParams, saveId, dayNumber, eggItemId);
                if (eggsToSpawn <= 0)
                    continue;

                var chance = chanceCalc.Calculate(kind, rule.UseBaseRandomChance, rule.RandomChance);

                var includeRects = RectParsing.ParseMany(rule.IncludeCoordinates, msg => _log.Log(msg, StardewModdingAPI.LogLevel.Trace));
                var excludeRects = RectParsing.ParseMany(rule.ExcludeCoordinates, msg => _log.Log(msg, StardewModdingAPI.LogLevel.Trace));

                var orderRng = new SeededRandomProvider(DeterministicSeed.For(saveId, dayNumber, "job-order", eggItemId, rule.ModID, rule.MapName));
                var key = chance + orderRng.NextDouble();

                jobsWithKey.Add((
                    key,
                    new SpawnJob(
                        EggItemId: eggItemId,
                        EggKind: kind,
                        PackId: rule.ModID,
                        LocationType: rule.Location,
                        MapName: rule.MapName,
                        SpawnChance: chance,
                        BypassRandomChance: false,
                        EggsToSpawn: eggsToSpawn,
                        IncludeTerrainTypes: rule.IncludeTerrainTypes,
                        ExcludeTerrainTypes: rule.ExcludeTerrainTypes,
                        IncludeRects: includeRects,
                        ExcludeRects: excludeRects)));
            }
        }

        var ordered = jobsWithKey
            .OrderByDescending(x => x.key)
            .Select(x => x.job)
            .ToList();

        MarkCertainJobs(ordered, parameters);

        ordered = FilterJobsByAreaChance(ordered, saveId, dayNumber);

        return new SpawnPlan(packBudgets, ordered);
    }

    private Dictionary<string, int> BuildPackBudgets(
        IReadOnlyDictionary<string, LocationPackJson> packs,
        SpawnParametersJson parameters,
        long saveId,
        int dayNumber)
    {
        var budgets = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var (packId, pack) in packs)
        {
            var packParams = IsVanilla(packId) ? parameters.Parameters_Vanilla : parameters.Parameters_Packs;

            var min = pack.MinimumSpawnsPerDay;
            var max = Math.Max(min, pack.MaximumSpawnsPerDay);

            var rng = new SeededRandomProvider(DeterministicSeed.For(saveId, dayNumber, "pack-budget", packId));
            var today = rng.Next(min, max + 1);

            if (packParams.MaxEggPack > 0)
                today = Math.Min(today, packParams.MaxEggPack);

            budgets[packId] = Math.Max(0, today);
        }

        return budgets;
    }

    private static bool PackContains(LocationPackJson pack, string locationType, string mapName)
        => pack.Locations.TryGetValue(locationType, out var maps)
           && maps is not null
           && maps.Contains(mapName);

    private static bool IsVanilla(string packId)
        => string.Equals(packId, Vanilla, StringComparison.OrdinalIgnoreCase);

    private static bool IsExcludedLocation(string mapName)
        => mapName.Contains("DesertTomb", StringComparison.OrdinalIgnoreCase);

    private static int DetermineEggsToSpawn(SpawnAreaRuleJson rule, ParameterPackJson packParams, long saveId, int dayNumber, string eggItemId)
    {
        var seed = DeterministicSeed.For(saveId, dayNumber, "eggs-to-spawn", eggItemId, rule.ModID, rule.MapName);
        var rng = new SeededRandomProvider(seed);

        if (packParams.UseJsonValues)
        {
            if (rule.EggsToSpawn > 0)
                return rule.EggsToSpawn;

            var min = rule.MinEggs > 0 ? rule.MinEggs : packParams.MinEggArea;
            var max = rule.MaxEggs > 0 ? rule.MaxEggs : packParams.MaxEggArea;
            max = Math.Max(min, max);

            return rng.Next(Math.Max(0, min), Math.Max(0, max) + 1);
        }

        var pmin = Math.Max(0, packParams.MinEggArea);
        var pmax = Math.Max(pmin, packParams.MaxEggArea);
        return rng.Next(pmin, pmax + 1);
    }

    
    private List<SpawnJob> FilterJobsByAreaChance(List<SpawnJob> ordered, long saveId, int dayNumber)
    {
        var kept = new List<SpawnJob>(ordered.Count);

        foreach (var job in ordered)
        {
            if (job.BypassRandomChance || job.SpawnChance >= 1.0)
            {
                kept.Add(job);
                continue;
            }

            if (job.SpawnChance <= 0.0)
                continue;

            var rng = new SeededRandomProvider(
                DeterministicSeed.For(saveId, dayNumber, "job-chance", job.EggItemId, job.MapName, job.PackId));

            if (rng.NextDouble() < job.SpawnChance)
            {
                kept.Add(job);
                continue;
            }

            _log.Log(
                $"Spawn plan: excluded job due to random chance (job={job.EggItemId} map={job.MapName} chance={job.SpawnChance:0.###})",
                StardewModdingAPI.LogLevel.Trace);
        }

        return kept;
    }

private static void MarkCertainJobs(List<SpawnJob> jobs, SpawnParametersJson parameters)
    {
        var remainingVanilla = Math.Max(0, parameters.Parameters_Vanilla.CertainEggs);
        var remainingPacks = Math.Max(0, parameters.Parameters_Packs.CertainEggs);

        for (var i = 0; i < jobs.Count; i++)
        {
            var j = jobs[i];

            if (IsVanilla(j.PackId))
            {
                if (remainingVanilla <= 0)
                    continue;

                jobs[i] = j with { BypassRandomChance = true };
                remainingVanilla--;
                continue;
            }

            if (remainingPacks <= 0)
                continue;

            jobs[i] = j with { BypassRandomChance = true };
            remainingPacks--;
        }
    }
}
