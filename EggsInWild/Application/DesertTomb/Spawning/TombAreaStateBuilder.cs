using System;
using System.Collections.Generic;
using System.Linq;
using StardewValley.Locations;
using EggsInWild.DesertTomb.Core.Model;
using EggsInWild.DesertTomb.Core.Spawning;
using static EggsInWild.DesertTomb.Core.Enums.DesertTombEnums;

namespace EggsInWild.Application.DesertTomb.Spawning;

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
            var roles = new Dictionary<TombMonsterType, TombMonsterRole>();
            var allowed = new HashSet<TombMonsterType>();

            foreach (var s in areaPlan.Spawns)
            {
                allowed.Add(s.Type);
                desired[s.Type] = s.Count;
                stats[s.Type] = s.Stats.Clone();
                roles[s.Type] = roles.TryGetValue(s.Type, out var existing)
                    ? PreferRole(existing, s.Role)
                    : s.Role;
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
                    rolesByType: roles,
                    validSpawnTiles: validTiles,
                    minSpacingTiles: minSpacingTiles
                )
            );
        }

        return states;
    }

    private static TombMonsterRole PreferRole(TombMonsterRole existing, TombMonsterRole incoming)
    {
        if (existing == incoming)
            return existing;

        static int Rank(TombMonsterRole r) => r switch
        {
            TombMonsterRole.Guard => 3,
            TombMonsterRole.Patrol => 2,
            _ => 1,
        };

        return Rank(existing) >= Rank(incoming) ? existing : incoming;
    }
}
