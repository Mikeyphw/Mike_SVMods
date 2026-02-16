using System;
using System.Collections.Generic;
using System.Linq;
using WildEggsHatching.DesertTomb.Core.Configuration;
using WildEggsHatching.DesertTomb.Core.Model;
using static WildEggsHatching.DesertTomb.Core.Enums.DesertTombEnums;

namespace WildEggsHatching.DesertTomb.Core.Spawning;

/// <summary>
/// Pure planner that produces a <see cref="TombSpawnPlan"/> from configuration and a tomb layout.
/// </summary>
public sealed class TombSpawnPlanner
{
    private readonly IRandomProvider _rng;

    public TombSpawnPlanner(IRandomProvider rng)
    {
        _rng = rng ?? throw new ArgumentNullException(nameof(rng));
    }

    public TombSpawnPlan BuildPlan(TombLayout layout, SpawnConfiguration config, TombDifficulty difficulty)
    {
        if (layout is null)
            throw new ArgumentNullException(nameof(layout));
        if (config is null)
            throw new ArgumentNullException(nameof(config));

        var allowedByDifficulty = config.AllowedMonstersByDifficulty.TryGetValue(difficulty, out var allowed)
            ? allowed
            : null;

        var globalMaxByType = config.Global.MaxMonsterInTomb ?? new Dictionary<TombMonsterType, int>();
        var globalCounts = Enum.GetValues(typeof(TombMonsterType))
            .Cast<TombMonsterType>()
            .ToDictionary(x => x, _ => 0);

        var areaPlans = new List<TombAreaSpawnPlan>(layout.Areas.Count);

        foreach (var area in layout.Areas)
        {
            var areaType = area.Type;

            if (!config.MonsterSpawnRatesByArea.TryGetValue(areaType, out var statsByType))
            {
                // Fallback to NormalArea if not specified.
                config.MonsterSpawnRatesByArea.TryGetValue(TombAreaType.NormalArea, out statsByType);
            }

            if (statsByType is null || statsByType.Count == 0)
            {
                areaPlans.Add(new TombAreaSpawnPlan(area, areaType, Array.Empty<MonsterSpawnSpec>()));
                continue;
            }

            var candidates = area.AllowedMonsters
                .Where(t => allowedByDifficulty is null || allowedByDifficulty.Contains(t))
                .Where(t => statsByType.ContainsKey(t))
                .ToList();

            if (candidates.Count == 0)
            {
                areaPlans.Add(new TombAreaSpawnPlan(area, areaType, Array.Empty<MonsterSpawnSpec>()));
                continue;
            }

            var desiredCount = NextInclusive(area.Capacity.Min, area.Capacity.Max);
            desiredCount = Math.Clamp(desiredCount, config.Global.MinMonstersPerArea, config.Global.MaxMonstersPerArea);

            if (desiredCount <= 0)
            {
                areaPlans.Add(new TombAreaSpawnPlan(area, areaType, Array.Empty<MonsterSpawnSpec>()));
                continue;
            }

            // Select a dominant "guard" type, then up to 2 wander types.
            var guardType = ChooseWeighted(candidates, t => Math.Max(1, statsByType[t].Max));

            var guardCount = Math.Clamp((int)Math.Round(desiredCount * 0.7), 1, desiredCount);
            var remaining = desiredCount - guardCount;

            var specs = new List<MonsterSpawnSpec>(capacity: 4);

            AddWithGlobalCap(specs, globalCounts, globalMaxByType, guardType, guardCount, statsByType[guardType]);

            if (remaining > 0)
            {
                var wanderPool = candidates.Where(t => t != guardType).ToList();
                if (wanderPool.Count == 0)
                {
                    AddWithGlobalCap(specs, globalCounts, globalMaxByType, guardType, remaining, statsByType[guardType]);
                }
                else
                {
                    var wanderTypes = new List<TombMonsterType>(2);
                    wanderTypes.Add(ChooseWeighted(wanderPool, t => Math.Max(1, statsByType[t].Max)));
                    wanderPool.Remove(wanderTypes[0]);

                    if (wanderPool.Count > 0 && remaining >= 2 && _rng.NextDouble() < 0.6)
                    {
                        wanderTypes.Add(ChooseWeighted(wanderPool, t => Math.Max(1, statsByType[t].Max)));
                    }

                    if (wanderTypes.Count == 1)
                    {
                        AddWithGlobalCap(specs, globalCounts, globalMaxByType, wanderTypes[0], remaining, statsByType[wanderTypes[0]]);
                    }
                    else
                    {
                        var first = NextInclusive(1, remaining - 1);
                        var second = remaining - first;
                        AddWithGlobalCap(specs, globalCounts, globalMaxByType, wanderTypes[0], first, statsByType[wanderTypes[0]]);
                        AddWithGlobalCap(specs, globalCounts, globalMaxByType, wanderTypes[1], second, statsByType[wanderTypes[1]]);
                    }
                }
            }

            areaPlans.Add(new TombAreaSpawnPlan(area, areaType, specs));
        }

        return new TombSpawnPlan(areaPlans);
    }

    private int NextInclusive(int minInclusive, int maxInclusive)
    {
        if (maxInclusive < minInclusive)
            (minInclusive, maxInclusive) = (maxInclusive, minInclusive);

        // Random.Next max is exclusive
        return _rng.Next(minInclusive, maxInclusive + 1);
    }

    private TombMonsterType ChooseWeighted(IReadOnlyList<TombMonsterType> candidates, Func<TombMonsterType, int> weight)
    {
        var total = 0;
        foreach (var c in candidates)
            total += Math.Max(1, weight(c));

        var roll = _rng.Next(0, total);
        var cumulative = 0;

        foreach (var c in candidates)
        {
            cumulative += Math.Max(1, weight(c));
            if (roll < cumulative)
                return c;
        }

        return candidates[^1];
    }

    private static void AddWithGlobalCap(
        List<MonsterSpawnSpec> specs,
        Dictionary<TombMonsterType, int> globalCounts,
        IReadOnlyDictionary<TombMonsterType, int> globalMaxByType,
        TombMonsterType type,
        int desiredCount,
        MonsterSpawnStats stats
    )
    {
        if (desiredCount <= 0)
            return;

        var maxForType = globalMaxByType.TryGetValue(type, out var m) ? m : int.MaxValue;
        var current = globalCounts[type];
        var remaining = maxForType - current;
        if (remaining <= 0)
            return;

        var count = Math.Min(desiredCount, remaining);
        if (count <= 0)
            return;

        globalCounts[type] = current + count;

        // Cap to per-type max in the area stats as well.
        count = Math.Min(count, Math.Max(1, stats.Max));

        specs.Add(new MonsterSpawnSpec(type, count, stats.Clone()));
    }
}
