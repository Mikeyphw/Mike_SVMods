using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using static WildEggsHatching.DesertTomb.Core.Enums.DesertTombEnums;

namespace WildEggsHatching.DesertTomb.Core.Spawning;

/// <summary>
/// Full-tomb spawn plan (pure). Execution is handled in Application/Infrastructure.
/// </summary>
public sealed class TombSpawnPlan
{
    public IReadOnlyList<TombAreaSpawnPlan> Areas { get; }

    public IReadOnlyDictionary<TombMonsterType, int> TotalsByMonsterType { get; }

    public TombSpawnPlan(IReadOnlyList<TombAreaSpawnPlan> areas)
    {
        Areas = areas;

        var totals = new Dictionary<TombMonsterType, int>();
        foreach (var area in areas)
        {
            foreach (var spec in area.Spawns)
            {
                totals[spec.Type] = totals.TryGetValue(spec.Type, out var cur) ? cur + spec.Count : spec.Count;
            }
        }

        TotalsByMonsterType = new ReadOnlyDictionary<TombMonsterType, int>(totals);
    }

    public int TotalCount => TotalsByMonsterType.Values.Sum();
}
