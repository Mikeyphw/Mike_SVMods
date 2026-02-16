using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
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
            {
                // Defensive fallback; should not happen when built via TombAreaStateBuilder.
                areaState = new TombAreaState(
                    areaType: areaPlan.AreaType,
                    rect: rect,
                    allowedTypesResolved: TombAreaState.TypesFromPlan(Array.Empty<TombMonsterType>()),
                    capacityChosen: 0,
                    validSpawnTiles: TombSpawnTileCache.ComputeValidSpawnTiles(mineShaft, rect)
                );
                statesById[areaId] = areaState;
            }

            // Phase 2: compute valid tiles once per area and consume them while spawning.
            var cache = new TombSpawnTileCache(
                candidates: new List<Point>(areaState.ValidSpawnTiles),
                minSpacingTiles: 0
            );

            foreach (var spec in areaPlan.Spawns)
            {
                for (int i = 0; i < spec.Count; i++)
                {
                    if (!cache.TryTakeTile(mineShaft, out var tile))
                    {
                        _log.Log(
                            $"[DesertTomb] No valid spawn tile for {spec.Type} in area {rect}. " +
                            $"SpawnCount={spec.Count}, MineLevel={mineShaft.mineLevel}",
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

                    mineShaft.characters.Add(monster);
                    areaState.IncrementSpawned(spec.Type);
                    spawned++;
                }
            }
        }

        return spawned;
    }
}
