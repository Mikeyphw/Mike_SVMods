using System;
using System.Collections.Generic;
using System.Linq;
using EggsInWild.DesertTomb.Core.Configuration;
using EggsInWild.DesertTomb.Core.Model;
using static EggsInWild.DesertTomb.Core.Enums.DesertTombEnums;

namespace EggsInWild.DesertTomb.Core.Spawning;

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
        => BuildPlan(
            layout: layout,
            config: config,
            difficulty: difficulty,
            progressionFlags: TombSpawnProgressionFlags.Disabled,
            mapTags: null
        );

    public TombSpawnPlan BuildPlan(
        TombLayout layout,
        SpawnConfiguration config,
        TombDifficulty difficulty,
        TombSpawnProgressionFlags progressionFlags,
        IReadOnlySet<string>? mapTags
    )
    {
        if (layout is null)
            throw new ArgumentNullException(nameof(layout));
        if (config is null)
            throw new ArgumentNullException(nameof(config));

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

            var allowed = AllowedMonsterResolver.ResolveAllowedMonsters(
                areaType: areaType,
                difficulty: difficulty,
                progressionFlags: progressionFlags,
                mapTags: mapTags,
                areaHardAllowlist: area.AllowedMonsters,
                config: config
            );

            var candidates = allowed.Where(t => statsByType.ContainsKey(t)).ToList();
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

            var guardType = ChooseWeighted(candidates, t => Math.Max(1, statsByType[t].Weight));
            var guardCount = Math.Clamp((int)Math.Round(desiredCount * 0.7), 1, desiredCount);
            var remaining = desiredCount - guardCount;

            var specs = new List<MonsterSpawnSpec>(capacity: 4);

            AddWithCaps(
                specs: specs,
                globalCounts: globalCounts,
                globalMaxByType: globalMaxByType,
                type: guardType,
                desiredCount: guardCount,
                stats: statsByType[guardType],
                role: TombMonsterRole.Guard
            );

            if (remaining > 0)
            {
                var wanderPool = candidates.Where(t => t != guardType).ToList();
                if (wanderPool.Count == 0)
                {
                    AddWithCaps(specs, globalCounts, globalMaxByType, guardType, remaining, statsByType[guardType], TombMonsterRole.Guard);
                }
                else
                {
                    var wanderTypes = new List<TombMonsterType>(2)
                    {
                        ChooseWeighted(wanderPool, t => Math.Max(1, statsByType[t].Weight)),
                    };
                    wanderPool.Remove(wanderTypes[0]);

                    if (wanderPool.Count > 0 && remaining >= 2 && _rng.NextDouble() < 0.6)
                        wanderTypes.Add(ChooseWeighted(wanderPool, t => Math.Max(1, statsByType[t].Weight)));

                    if (wanderTypes.Count == 1)
                    {
                        AddWithCaps(specs, globalCounts, globalMaxByType, wanderTypes[0], remaining, statsByType[wanderTypes[0]], TombMonsterRole.Wander);
                    }
                    else
                    {
                        var first = NextInclusive(1, remaining - 1);
                        var second = remaining - first;
                        AddWithCaps(specs, globalCounts, globalMaxByType, wanderTypes[0], first, statsByType[wanderTypes[0]], TombMonsterRole.Wander);
                        AddWithCaps(specs, globalCounts, globalMaxByType, wanderTypes[1], second, statsByType[wanderTypes[1]], TombMonsterRole.Wander);
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

        // Random.Next max is exclusive.
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


    private static TombMonsterRole PreferRole(TombMonsterRole existing, TombMonsterRole incoming)
    {
        if (existing == incoming)
            return existing;

        // Guard > Patrol > Wander
        static int Rank(TombMonsterRole r) => r switch
        {
            TombMonsterRole.Guard => 3,
            TombMonsterRole.Patrol => 2,
            _ => 1,
        };

        return Rank(existing) >= Rank(incoming) ? existing : incoming;
    }

    private static void AddWithCaps(
        List<MonsterSpawnSpec> specs,
        Dictionary<TombMonsterType, int> globalCounts,
        IReadOnlyDictionary<TombMonsterType, int> globalMaxByType,
        TombMonsterType type,
        int desiredCount,
        MonsterSpawnStats stats,
        TombMonsterRole role
    )
    {
        if (desiredCount <= 0)
            return;

        var maxForTypeGlobal = globalMaxByType.TryGetValue(type, out var m) ? m : int.MaxValue;
        var currentGlobal = globalCounts[type];
        var remainingGlobal = maxForTypeGlobal - currentGlobal;
        if (remainingGlobal <= 0)
            return;

        var maxPerArea = stats.MaxPerArea <= 0 ? int.MaxValue : stats.MaxPerArea;
        var currentInArea = 0;
        for (int i = 0; i < specs.Count; i++)
        {
            if (specs[i].Type == type)
                currentInArea += specs[i].Count;
        }

        var remainingArea = maxPerArea - currentInArea;
        if (remainingArea <= 0)
            return;

        var count = Math.Min(desiredCount, Math.Min(remainingGlobal, remainingArea));
        if (count <= 0)
            return;

        globalCounts[type] = currentGlobal + count;

        // Merge if spec already exists for this type.
        for (int i = 0; i < specs.Count; i++)
        {
            if (specs[i].Type != type)
                continue;

            var merged = specs[i] with { Count = specs[i].Count + count };
            specs[i] = merged;
            return;
        }

        specs.Add(new MonsterSpawnSpec(type, count, stats.Clone(), role));
    }
}
