using Microsoft.Xna.Framework;
using static EggsInWild.DesertTomb.Core.Enums.DesertTombEnums;

namespace EggsInWild.DesertTomb.Core.Model;

/// <summary>
/// Immutable layout definition for a tomb area (geometry + constraints).
/// Runtime state (occupancy, chosen capacity, valid tiles) lives outside Core.
/// </summary>
public sealed class TombAreaLayout
{
    public Rectangle Area { get; }
    public TombAreaType Type { get; }
    public CapacityRange Capacity { get; }
    public IReadOnlySet<TombMonsterType> AllowedMonsters { get; }

    public TombAreaLayout(
        Rectangle area,
        TombAreaType type = TombAreaType.NormalArea,
        CapacityRange? capacity = null,
        IReadOnlySet<TombMonsterType>? allowedMonsters = null
    )
    {
        Area = area;
        Type = type;
        Capacity = capacity ?? GetDefaultCapacity(type);
        AllowedMonsters = allowedMonsters ?? GetDefaultAllowedMonsters(type);
    }

    private static CapacityRange GetDefaultCapacity(TombAreaType areaType) =>
        areaType switch
        {
            TombAreaType.DuggyArea or TombAreaType.SmallArea => new CapacityRange(1, 2),
            _ => new CapacityRange(1, 1),
        };

    private static IReadOnlySet<TombMonsterType> GetDefaultAllowedMonsters(TombAreaType areaType) =>
        areaType switch
        {
            TombAreaType.DuggyArea => new HashSet<TombMonsterType> { TombMonsterType.Duggy, TombMonsterType.Hurricane, TombMonsterType.Crawler, TombMonsterType.Matango },
            TombAreaType.MummyArea => new HashSet<TombMonsterType> { TombMonsterType.TheTombPharaoh },
            TombAreaType.SmallArea => new HashSet<TombMonsterType>
            {
                TombMonsterType.Mummy,
                TombMonsterType.AssassinBug,
                TombMonsterType.Spider,
                TombMonsterType.Hurricane,
                TombMonsterType.Crawler,
                TombMonsterType.Matango,
            },
            _ => new HashSet<TombMonsterType>(),
        };
}
