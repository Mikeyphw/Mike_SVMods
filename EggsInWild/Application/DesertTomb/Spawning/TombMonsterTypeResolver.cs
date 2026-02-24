using System;
using StardewValley.Monsters;
using EggsInWild.Infrastructure.DesertTomb.Monsters.Creatures;
using static EggsInWild.DesertTomb.Core.Enums.DesertTombEnums;

namespace EggsInWild.Application.DesertTomb.Spawning;

/// <summary>
/// Best-effort mapping from a runtime <see cref="Monster"/> instance to a tomb monster type.
/// </summary>
public static class TombMonsterTypeResolver
{
    private const string MonsterTypeKey = Constants.ModId + "/deserttomb/type";

    public static bool TryGetTombMonsterType(Monster monster, out TombMonsterType type)
    {
        if (monster is null)
            throw new ArgumentNullException(nameof(monster));

        if (monster.modData.TryGetValue(MonsterTypeKey, out var value)
            && Enum.TryParse(value, ignoreCase: false, out TombMonsterType parsed))
        {
            type = parsed;
            return true;
        }

        // Fallback to type checks for compatibility with existing instances.
        type = monster switch
        {
            TombAssassinBug => TombMonsterType.AssassinBug,
            TombBat => TombMonsterType.Bat,
            BruteMummy => TombMonsterType.BruteMummy,
            TombDuggy => TombMonsterType.Duggy,
            TombHauntedSkull => TombMonsterType.HauntedSkull,
            TombGhost => TombMonsterType.Ghost,
            TombMummy => TombMonsterType.Mummy,
            TombSerpent => TombMonsterType.RoyalSerpent,
            TombSkeleton => TombMonsterType.Skeleton,
            TombSkeletonMage => TombMonsterType.SkeletonMage,
            TombSpider => TombMonsterType.Spider,
            ThePharaoh => TombMonsterType.TheTombPharaoh,
            Crawler => TombMonsterType.Crawler,
            Matango => TombMonsterType.Matango,
            Hurricane => TombMonsterType.Hurricane,
            Sandi => TombMonsterType.Sandi,
            _ => default,
        };

        return type != default;
    }
}
