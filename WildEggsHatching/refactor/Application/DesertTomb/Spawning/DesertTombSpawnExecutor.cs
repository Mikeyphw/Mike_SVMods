using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Locations;
using WildEggsHatching.DesertTomb.Core.Configuration;
using WildEggsHatching.DesertTomb.Core.Spawning;
using WildEggsHatching.Infrastructure.DesertTomb.Monsters;
using WildEggsHatching.Infrastructure.Logging;
using static WildEggsHatching.DesertTomb.Core.Enums.DesertTombEnums;

namespace WildEggsHatching.Application.DesertTomb.Spawning;

/// <summary>
/// Executes a pure <see cref="TombSpawnPlan"/> against a live <see cref="MineShaft"/> by:
/// - choosing concrete spawn tiles within each area, and
/// - creating monsters via <see cref="IDesertTombMonsterFactory"/>.
/// </summary>
public sealed class DesertTombSpawnExecutor
{

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

                monster.modData[TombMonsterBehaviorModData.MonsterTypeKey] = spec.Type.ToString();
                monster.modData[TombMonsterBehaviorModData.SpawnAreaIdKey] = areaState.AreaId.ToString();
                if (spec.Type != TombMonsterType.TheTombPharaoh)
                {
                    var role = spec.Role;
                    monster.modData[TombMonsterBehaviorModData.RoleKey] = role.ToString();

                    monster.modData[TombMonsterBehaviorModData.HomeTileKey] = TombMonsterBehaviorModData.FormatPoint(tile);

                    var leashRadius = ComputeLeashRadius(areaState.Rect, role);
                    monster.modData[TombMonsterBehaviorModData.LeashRadiusKey] = leashRadius.ToString();

                    if (role == MonsterSpawnRole.Guard || role == MonsterSpawnRole.Patrol)
                        monster.modData[TombMonsterBehaviorModData.LeashRectKey] = TombMonsterBehaviorModData.FormatRect(areaState.Rect);

                    monster.modData.Remove(TombMonsterBehaviorModData.LeashTicksKey);
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

    private static bool IsFarFromPlayer(Point tile, int minDistanceTiles)
    {
        if (minDistanceTiles <= 0)
            return true;

        // Main player is authoritative for spawns in our runtime.
        var player = Game1.player;
        if (player is null)
            return true;

        var playerTile = player.getTileLocationPoint();
        var dx = tile.X - playerTile.X;
        var dy = tile.Y - playerTile.Y;
        return (dx * dx + dy * dy) >= (minDistanceTiles * minDistanceTiles);
    }
}
