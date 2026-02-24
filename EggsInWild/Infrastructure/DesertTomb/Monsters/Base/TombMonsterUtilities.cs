using StardewValley.Monsters;
using EggsInWild.Infrastructure.DesertTomb.Monsters.Creatures;
using static EggsInWild.DesertTomb.Core.Enums.DesertTombEnums;

namespace EggsInWild.Infrastructure.DesertTomb.Monsters.Base;

internal static class TombMonsterUtilities
{
    public static TombMonsterType GetMonsterType(Monster monster) =>
        monster switch
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
            _ => TombMonsterType.Mummy,
        };
}
