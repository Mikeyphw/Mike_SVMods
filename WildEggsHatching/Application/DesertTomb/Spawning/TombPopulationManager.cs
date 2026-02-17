using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Monsters;
using WildEggsHatching.DesertTomb.Core.Configuration;
using WildEggsHatching.DesertTomb.Core.Model;
using WildEggsHatching.DesertTomb.Core.Spawning;
using WildEggsHatching.Infrastructure.Logging;
using static WildEggsHatching.DesertTomb.Core.Enums.DesertTombEnums;

namespace WildEggsHatching.Application.DesertTomb.Spawning;

/// <summary>
/// Runtime population manager that applies a pure spawn plan to a live floor over time,
/// enforcing live caps and pacing spawns to avoid big pop-in spikes.
/// </summary>
public sealed class TombPopulationManager
{
    private readonly MineShaft _mineShaft;
    private readonly TombSpawnPlan _plan;
    private readonly SpawnConfiguration _config;
    private readonly IReadOnlyList<TombAreaState> _areas;
    private readonly DesertTombSpawnExecutor _executor;
    private readonly ILog _log;

    private readonly Dictionary<int, TombAreaState> _areasById;

    private readonly double _initialWaveFraction;
    private readonly int _reconcileIntervalTicks;
    private readonly int _maxSpawnsPerTick;
    private readonly int _minDistanceToPlayerTiles;

    private bool _initialFillCompleted;
    private readonly bool _allowRespawnAfterInitialFill;

    private int _ticks;

    public TombPopulationManager(
        MineShaft mineShaft,
        TombSpawnPlan plan,
        SpawnConfiguration config,
        IReadOnlyList<TombAreaState> areas,
        DesertTombSpawnExecutor executor,
        ILog log,
        double initialWaveFraction = 0.75,
        int reconcileIntervalTicks = 30,
        int maxSpawnsPerTick = 6,
        int minDistanceToPlayerTiles = 5,
        bool allowRespawnAfterInitialFill = false
    )
    {
        _mineShaft = mineShaft ?? throw new ArgumentNullException(nameof(mineShaft));
        _plan = plan ?? throw new ArgumentNullException(nameof(plan));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _areas = areas ?? throw new ArgumentNullException(nameof(areas));
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
        _log = log ?? throw new ArgumentNullException(nameof(log));

        _areasById = _areas.ToDictionary(a => a.AreaId, a => a);

        _initialWaveFraction = Math.Clamp(initialWaveFraction, 0.0, 1.0);
        _reconcileIntervalTicks = Math.Max(1, reconcileIntervalTicks);
        _maxSpawnsPerTick = Math.Max(1, maxSpawnsPerTick);
        _minDistanceToPlayerTiles = Math.Max(0, minDistanceToPlayerTiles);
        _allowRespawnAfterInitialFill = allowRespawnAfterInitialFill;
    }

    public bool IsInitialFillComplete => _initialFillCompleted;

    /// <summary>
    /// Spawn the initial wave (typically 60–80% of the planned population).
    /// Remaining population is paced in over subsequent ticks.
    /// </summary>
    public int SpawnInitialWave()
    {
        var spawned = 0;

        foreach (var areaPlan in _plan.Areas)
        {
            var areaId = TombAreaState.ComputeAreaId(areaPlan.Area.Area, areaPlan.AreaType);
            if (!_areasById.TryGetValue(areaId, out var state))
                continue;

            var wave = BuildFractionalWave(areaPlan.Spawns, _initialWaveFraction);

            if (wave.Count == 0)
                continue;

            spawned += _executor.SpawnIntoArea(
                mineShaft: _mineShaft,
                areaState: state,
                specs: wave,
                config: _config,
                minDistanceToPlayerTiles: _minDistanceToPlayerTiles,
                maxSpawnCount: int.MaxValue
            );
        }

        return spawned;
    }

    /// <summary>
    /// Call once per tick while the player remains on the floor.
    /// </summary>
    public void UpdateTicked()
    {
        _ticks++;

        if (!ReferenceEquals(Game1.currentLocation, _mineShaft))
            return;

        if (!_allowRespawnAfterInitialFill && _initialFillCompleted)
            return;

        if (_ticks % _reconcileIntervalTicks != 0)
            return;

        try
        {
            ReconcileAndSpawn();
        }
        catch (Exception ex)
        {
            _log.Error($"[DesertTomb] Population reconcile failed: {ex}");
        }
    }

    private void ReconcileAndSpawn()
    {
        var desiredGlobal = BuildDesiredGlobalCounts(_areas);
        var liveGlobal = CountLiveByType(_mineShaft);

        var globalDeficit = new Dictionary<TombMonsterType, int>();
        foreach (var (type, desired) in desiredGlobal)
        {
            var live = liveGlobal.TryGetValue(type, out var v) ? v : 0;
            var deficit = Math.Max(0, desired - live);

            var cap = _config.Global.MaxMonsterInTomb.TryGetValue(type, out var max) ? max : int.MaxValue;
            var remainingCap = Math.Max(0, cap - live);

            deficit = Math.Min(deficit, remainingCap);

            if (deficit > 0)
                globalDeficit[type] = deficit;
        }

        if (globalDeficit.Count == 0)
        {
            _initialFillCompleted = true;
            return;
        }

        UpdateAreaLiveCounts(_mineShaft, _areas);

        var budget = _maxSpawnsPerTick;

        foreach (var area in _areas)
        {
            if (budget <= 0)
                break;

            var toSpawn = AllocateAreaWave(area, globalDeficit, budget);
            if (toSpawn.Count == 0)
                continue;

            var spawnedByType = new Dictionary<TombMonsterType, int>();

            var spawned = _executor.SpawnIntoArea(
                mineShaft: _mineShaft,
                areaState: area,
                specs: toSpawn,
                config: _config,
                minDistanceToPlayerTiles: _minDistanceToPlayerTiles,
                maxSpawnCount: budget,
                spawnedByType: spawnedByType
            );

            budget -= spawned;

            foreach (var (type, spawnedCount) in spawnedByType)
            {
                if (!globalDeficit.TryGetValue(type, out var remaining))
                    continue;

                remaining -= spawnedCount;
                if (remaining <= 0)
                    globalDeficit.Remove(type);
                else
                    globalDeficit[type] = remaining;
            }

            if (globalDeficit.Count == 0)
            {
                _initialFillCompleted = true;
                return;
            }
        }
    }

    private static Dictionary<TombMonsterType, int> BuildDesiredGlobalCounts(IReadOnlyList<TombAreaState> areas)
    {
        var result = new Dictionary<TombMonsterType, int>();

        foreach (var area in areas)
        {
            foreach (var (type, desired) in area.DesiredCountByType)
            {
                if (desired <= 0)
                    continue;

                result[type] = (result.TryGetValue(type, out var current) ? current : 0) + desired;
            }
        }

        return result;
    }

    private static void UpdateAreaLiveCounts(MineShaft mineShaft, IReadOnlyList<TombAreaState> areas)
    {
        foreach (var a in areas)
            a.AliveCountByType.Clear();

        foreach (var c in mineShaft.characters)
        {
            if (c is not Monster m)
                continue;

            if (!TombMonsterTypeResolver.TryGetTombMonsterType(m, out var type))
                continue;

            var tile = m.getTileLocationPoint();

            for (int i = 0; i < areas.Count; i++)
            {
                var area = areas[i];
                if (!area.Rect.Contains(tile))
                    continue;

                area.SetAliveCount(type, (area.AliveCountByType.TryGetValue(type, out var current) ? current : 0) + 1);
                break;
            }
        }
    }

    private static Dictionary<TombMonsterType, int> CountLiveByType(MineShaft mineShaft)
    {
        var result = new Dictionary<TombMonsterType, int>();

        foreach (var c in mineShaft.characters)
        {
            if (c is not Monster m)
                continue;

            if (!TombMonsterTypeResolver.TryGetTombMonsterType(m, out var type))
                continue;

            result[type] = (result.TryGetValue(type, out var current) ? current : 0) + 1;
        }

        return result;
    }

    private List<MonsterSpawnSpec> AllocateAreaWave(
        TombAreaState area,
        Dictionary<TombMonsterType, int> globalDeficit,
        int budget
    )
    {
        var list = new List<MonsterSpawnSpec>();

        foreach (var (type, desiredInArea) in area.DesiredCountByType)
        {
            if (budget <= 0)
                break;

            if (!globalDeficit.TryGetValue(type, out var globalRemaining) || globalRemaining <= 0)
                continue;

            var liveInArea = area.AliveCountByType.TryGetValue(type, out var current) ? current : 0;
            var areaDeficit = Math.Max(0, desiredInArea - liveInArea);

            if (areaDeficit <= 0)
                continue;

            var spawnCount = Math.Min(areaDeficit, Math.Min(globalRemaining, budget));
            if (spawnCount <= 0)
                continue;

            if (!area.StatsByType.TryGetValue(type, out var stats))
                continue;

            list.Add(new MonsterSpawnSpec(type, spawnCount, stats.Clone(), area.RolesByType.TryGetValue(type, out var r) ? r : TombMonsterRole.Wander));
            budget -= spawnCount;
        }

        return list;
    }

    private static List<MonsterSpawnSpec> BuildFractionalWave(IReadOnlyList<MonsterSpawnSpec> specs, double fraction)
    {
        var list = new List<MonsterSpawnSpec>(specs.Count);

        foreach (var s in specs)
        {
            if (s.Count <= 0)
                continue;

            var count = (int)Math.Floor(s.Count * fraction);
            count = Math.Clamp(count, 0, s.Count);

            if (count <= 0)
                continue;

            list.Add(new MonsterSpawnSpec(s.Type, count, s.Stats.Clone(), s.Role));
        }

        return list;
    }
}
