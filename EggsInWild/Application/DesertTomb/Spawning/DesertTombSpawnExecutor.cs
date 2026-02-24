using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Locations;
using EggsInWild.DesertTomb.Core.Configuration;
using EggsInWild.DesertTomb.Core.Spawning;
using EggsInWild.Infrastructure.DesertTomb.Monsters;
using Mikew.NestCore.Helpers.Logging;
using static EggsInWild.DesertTomb.Core.Enums.DesertTombEnums;

namespace EggsInWild.Application.DesertTomb.Spawning;

/// <summary>
/// Executes a pure <see cref="TombSpawnPlan"/> against a live <see cref="MineShaft"/> by:
/// - choosing concrete spawn tiles within each area, and
/// - creating monsters via <see cref="IDesertTombMonsterFactory"/>.
/// </summary>
public sealed class DesertTombSpawnExecutor
{
    private const string MonsterTypeKey = Constants.ModId + "/deserttomb/type";
    private const string SpawnAreaIdKey = Constants.ModId + "/deserttomb/areaId";
    private const string RoleKey = Constants.ModId + "/deserttomb/role";
    private const string HomeTileKey = Constants.ModId + "/deserttomb/homeTile";
    private const string LeashRadiusKey = Constants.ModId + "/deserttomb/leashRadius";
    private const string LeashRectKey = Constants.ModId + "/deserttomb/leashRect";
    private const string PatrolPointsKey = Constants.ModId + "/deserttomb/patrolPoints";
    private const string PatrolIndexKey = Constants.ModId + "/deserttomb/patrolIndex";

    private readonly IDesertTombMonsterFactory _factory;
    private readonly ILog _log;

    public DesertTombSpawnExecutor(IDesertTombMonsterFactory factory, ILog log)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _log = log ?? throw new ArgumentNullException(nameof(log));
    }

    public int Execute(
        MineShaft mineShaft,
        TombSpawnPlan plan,
        SpawnConfiguration config,
        IReadOnlyList<TombAreaState> areaStates
    )
    {
        if (mineShaft is null)
            throw new ArgumentNullException(nameof(mineShaft));
        if (plan is null)
            throw new ArgumentNullException(nameof(plan));
        if (config is null)
            throw new ArgumentNullException(nameof(config));
        if (areaStates is null)
            throw new ArgumentNullException(nameof(areaStates));

        int spawned = 0;

        var statesById = new Dictionary<int, TombAreaState>(capacity: areaStates.Count);
        foreach (var s in areaStates)
            statesById[s.AreaId] = s;

        foreach (var areaPlan in plan.Areas)
        {
            var rect = areaPlan.Area.Area;
            var areaId = TombAreaState.ComputeAreaId(rect, areaPlan.AreaType);

            if (!statesById.TryGetValue(areaId, out var areaState))
                continue;

            spawned += SpawnIntoArea(
                mineShaft: mineShaft,
                areaState: areaState,
                specs: areaPlan.Spawns,
                config: config,
                minDistanceToPlayerTiles: 0,
                maxSpawnCount: int.MaxValue
            );
        }

        return spawned;
    }

    public int SpawnIntoArea(
        MineShaft mineShaft,
        TombAreaState areaState,
        IReadOnlyList<MonsterSpawnSpec> specs,
        SpawnConfiguration config,
        int minDistanceToPlayerTiles,
        int maxSpawnCount,
        Dictionary<TombMonsterType, int>? spawnedByType = null
    )
    {
        if (mineShaft is null)
            throw new ArgumentNullException(nameof(mineShaft));
        if (areaState is null)
            throw new ArgumentNullException(nameof(areaState));
        if (specs is null)
            throw new ArgumentNullException(nameof(specs));
        if (config is null)
            throw new ArgumentNullException(nameof(config));

        var spawned = 0;
        var max = Math.Max(0, maxSpawnCount);

        foreach (var spec in specs)
        {
            if (spawned >= max)
                break;

            for (int i = 0; i < spec.Count && spawned < max; i++)
            {
                if (!areaState.SpawnTiles.TryTakeTile(
                        mineShaft,
                        accept: t => IsFarFromPlayer(t, minDistanceToPlayerTiles),
                        out var tile
                    ))
                {
                    _log.Log(
                        $"[DesertTomb] No valid spawn tile for {spec.Type} in area {areaState.Rect}. " +
                        $"Requested={spec.Count}, RemainingTiles={areaState.SpawnTiles.Remaining}, MineLevel={mineShaft.mineLevel}",
                        LogLevel.Trace
                    );
                    break;
                }

                var pixelPos = new Vector2(tile.X, tile.Y) * 64f;

                var monster = _factory.Create(
                    monsterType: spec.Type,
                    position: pixelPos,
                    difficulty: spec.Stats.DifficultyAdd,
                    speedToAdd: spec.Stats.SpeedAdd,
                    multipliers: config.Multipliers,
                    customDropData: null
                );

                monster.modData[MonsterTypeKey] = spec.Type.ToString();
                monster.modData[SpawnAreaIdKey] = areaState.AreaId.ToString();
                monster.modData[RoleKey] = spec.Role.ToString();
                monster.modData[HomeTileKey] = $"{tile.X},{tile.Y}";
                monster.modData[LeashRectKey] = $"{areaState.Rect.X},{areaState.Rect.Y},{areaState.Rect.Width},{areaState.Rect.Height}";
                monster.modData[LeashRadiusKey] = ComputeLeashRadiusTiles(areaState.Rect).ToString();

                if (spec.Role == TombMonsterRole.Patrol)
                {
                    monster.modData[PatrolPointsKey] = BuildPatrolPoints(mineShaft, areaState, tile);
                    monster.modData[PatrolIndexKey] = "0";
                }

                mineShaft.characters.Add(monster);
                areaState.IncrementSpawned(spec.Type);

                if (spawnedByType is not null)
                    spawnedByType[spec.Type] = (spawnedByType.TryGetValue(spec.Type, out var c) ? c : 0) + 1;

                spawned++;
            }
        }

        return spawned;
    }


    private static int ComputeLeashRadiusTiles(Rectangle rect)
    {
        // Keep it simple: bigger areas leash wider. Clamp to a sane range.
        var r = Math.Max(rect.Width, rect.Height);
        return Math.Clamp(r, 6, 20);
    }

    private static string BuildPatrolPoints(MineShaft mineShaft, TombAreaState areaState, Point homeTile)
    {
        // home + 1–3 additional points, spaced out a bit.
        var desired = mineShaft.mineRandom.Next(2, 5);
        var points = new List<Point>(capacity: desired) { homeTile };

        // Sample from valid tiles without consuming the area spawn cache.
        var candidates = areaState.ValidSpawnTiles;
        if (candidates.Count == 0)
            return $"{homeTile.X},{homeTile.Y}";

        var attempts = Math.Min(200, candidates.Count * 2);
        for (int i = 0; i < attempts && points.Count < desired; i++)
        {
            var candidate = candidates[mineShaft.mineRandom.Next(candidates.Count)];
            if (candidate == homeTile)
                continue;

            if (!TombSpawnTileCache.IsFarEnough(candidate, points, minSpacingTiles: 2))
                continue;

            points.Add(candidate);
        }

        // Serialize as "x,y;x,y;..."
        return string.Join(";", points.ConvertAll(p => $"{p.X},{p.Y}"));
    }

    private static bool IsFarFromPlayer(Point tile, int minDistanceTiles)
    {
        if (minDistanceTiles <= 0)
            return true;

        // Main player is authoritative for spawns in our runtime.
        var player = Game1.player;
        if (player is null)
            return true;

        var playerTile = player.TilePoint;
        var dx = tile.X - playerTile.X;
        var dy = tile.Y - playerTile.Y;
        return (dx * dx + dy * dy) >= (minDistanceTiles * minDistanceTiles);
    }
}
