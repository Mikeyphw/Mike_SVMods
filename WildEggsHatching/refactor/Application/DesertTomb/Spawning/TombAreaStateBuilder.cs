using System;
using System.Collections.Generic;
using System.Linq;
using StardewValley.Locations;
using WildEggsHatching.DesertTomb.Core.Spawning;

namespace WildEggsHatching.Application.DesertTomb.Spawning;

/// <summary>
/// Builds <see cref="TombAreaState"/> instances for a specific floor activation.
/// </summary>
public static class TombAreaStateBuilder
{
    public static List<TombAreaState> Build(MineShaft mineShaft, TombSpawnPlan plan)
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

            var allowed = TombAreaState.TypesFromPlan(areaPlan.Spawns.Select(s => s.Type));
            var capacity = areaPlan.Spawns.Sum(s => s.Count);

            states.Add(
                new TombAreaState(
                    areaType: areaPlan.AreaType,
                    rect: rect,
                    allowedTypesResolved: allowed,
                    capacityChosen: capacity,
                    validSpawnTiles: validTiles
                )
            );
        }

        return states;
    }
}
