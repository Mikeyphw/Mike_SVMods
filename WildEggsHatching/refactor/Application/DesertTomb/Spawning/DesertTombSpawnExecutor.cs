using System;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Locations;
using WildEggsHatching.DesertTomb.Core.Configuration;
using WildEggsHatching.DesertTomb.Core.Spawning;
using WildEggsHatching.Infrastructure.DesertTomb.Monsters;
using WildEggsHatching.Infrastructure.Logging;

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

    public int Execute(MineShaft mineShaft, TombSpawnPlan plan, SpawnConfiguration config)
    {
        if (mineShaft is null)
            throw new ArgumentNullException(nameof(mineShaft));
        if (plan is null)
            throw new ArgumentNullException(nameof(plan));
        if (config is null)
            throw new ArgumentNullException(nameof(config));

        int spawned = 0;

        foreach (var areaPlan in plan.Areas)
        {
            var rect = areaPlan.Area.Area;
            foreach (var spec in areaPlan.Spawns)
            {
                for (int i = 0; i < spec.Count; i++)
                {
                    var tile = TryFindSpawnTile(mineShaft, rect, maxAttempts: config.Global.MonsterSpawnAttemptsPerMonster);
                    if (tile is null)
                    {
                        _log.Trace(
                            $"[DesertTomb] No valid spawn tile for {spec.Type} in area {rect}. " +
                            $"SpawnCount={spec.Count}, MineLevel={mineShaft.mineLevel}"
                        );
                        continue;
                    }

                    var pixelPos = tile.Value * 64f;
                    var monster = _factory.Create(
                        monsterType: spec.Type,
                        position: pixelPos,
                        difficulty: spec.Stats.Difficulty,
                        speedToAdd: spec.Stats.Speed,
                        multipliers: config.Multipliers,
                        customDropData: null
                    );

                    mineShaft.characters.Add(monster);
                    spawned++;
                }
            }
        }

        return spawned;
    }

    private static Vector2? TryFindSpawnTile(MineShaft mineShaft, Rectangle area, int maxAttempts)
    {
        maxAttempts = Math.Max(1, maxAttempts);

        for (int i = 0; i < maxAttempts; i++)
        {
            int x = mineShaft.mineRandom.Next(area.X, area.Right);
            int y = mineShaft.mineRandom.Next(area.Y, area.Bottom);

            if (!mineShaft.isTileOnMap(x, y))
                continue;

            if (!mineShaft.isTileClearForMineObjects(x, y))
                continue;

            var tile = new Vector2(x, y);
            if (mineShaft.isTileOccupied(tile))
                continue;

            return tile;
        }

        return null;
    }
}
