using System;
using System.Collections.Generic;
using System.Linq;
using StardewValley.Locations;
using WildEggsHatching.DesertTomb.Core.Model;
using WildEggsHatching.DesertTomb.Core.Spawning;
using static WildEggsHatching.DesertTomb.Core.Enums.DesertTombEnums;

namespace WildEggsHatching.Application.DesertTomb.Spawning;

/// <summary>
/// Builds <see cref="TombAreaState"/> instances for a specific floor activation.
/// </summary>
public static class TombAreaStateBuilder
{
    public static List<TombAreaState> Build(MineShaft mineShaft, TombSpawnPlan plan, int minSpacingTiles = 0)
    {
        if (mineShaft is null)
            throw new ArgumentNullException(nameof(mineShaft));
        if (plan is null)
            throw new ArgumentNullException(nameof(plan));

        var states = new List<TombAreaState>(plan.Areas.Count);

        foreach (var areaPlan in plan.Areas)
        {
            var rect = areaPlan.Area.Area;

            var validTiles = TombSpawnTileCache.ComputeValidSpawnTiles(mineShaft, rect);

            var desired = new Dictionary<TombMonsterType, int>();
            var stats = new Dictionary<TombMonsterType, MonsterSpawnStats>();
            var allowed = new HashSet<TombMonsterType>();

            foreach (var s in areaPlan.Spawns)
            {
                allowed.Add(s.Type);
                desired[s.Type] = s.Count;
                stats[s.Type] = s.Stats.Clone();
            }

            var capacity = desired.Values.Sum();

            states.Add(
                new TombAreaState(
                    areaType: areaPlan.AreaType,
                    rect: rect,
                    allowedTypesResolved: allowed,
                    capacityChosen: capacity,
                    desiredCountByType: desired,
                    statsByType: stats,
                    validSpawnTiles: validTiles,
                    minSpacingTiles: minSpacingTiles
                )
            );
        }

        return states;
    }
}
