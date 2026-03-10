using System.Collections.ObjectModel;
using Microsoft.Xna.Framework;
using static EggsInWild.DesertTomb.Core.Enums.DesertTombEnums;

namespace EggsInWild.DesertTomb.Core.Model;

/// <summary>
/// Default tomb layouts ported from the prior DesertTomb implementation.
/// </summary>
public static class DefaultTombLayouts
{
    public static IReadOnlyDictionary<MineLevel, TombLayout> ByLevel { get; } =
        new ReadOnlyDictionary<MineLevel, TombLayout>(
            new Dictionary<MineLevel, TombLayout>
            {
                [MineLevel.Level14] = CreateLevel14(),
                [MineLevel.Level19] = CreateLevel19(),
                [MineLevel.Level28] = CreateLevel28(),
                [MineLevel.Level39] = CreateLevel39(),
                [MineLevel.Level77377] = CreateLevel77377(),
            }
        );

    private static TombLayout CreateLevel14() =>
        new(
            new TombAreaLayout[]
            {
                new(
                    area: new Rectangle(32, 5, 14, 15),
                    capacity: new CapacityRange(4, 6),
                    allowedMonsters: new HashSet<TombMonsterType> { TombMonsterType.Mummy, TombMonsterType.Spider, TombMonsterType.Hurricane, TombMonsterType.Crawler, TombMonsterType.Matango }
                ),
                new(
                    area: new Rectangle(32, 20, 14, 18),
                    capacity: new CapacityRange(4, 6),
                    allowedMonsters: new HashSet<TombMonsterType>
                    {
                        TombMonsterType.Mummy,
                        TombMonsterType.AssassinBug,
                        TombMonsterType.Bat,
                        TombMonsterType.Hurricane, TombMonsterType.Crawler, TombMonsterType.Matango,
                    }
                ),
                new(
                    area: new Rectangle(2, 26, 15, 11),
                    capacity: new CapacityRange(1, 3),
                    allowedMonsters: new HashSet<TombMonsterType>
                    {
                        TombMonsterType.Mummy,
                        TombMonsterType.AssassinBug,
                        TombMonsterType.Spider,
                        TombMonsterType.BruteMummy,
                        TombMonsterType.Skeleton,
                        TombMonsterType.SkeletonMage,
                        TombMonsterType.RoyalSerpent,
                        TombMonsterType.HauntedSkull,
                        TombMonsterType.Hurricane, TombMonsterType.Crawler, TombMonsterType.Matango,
                    }
                ),
                new(
                    area: new Rectangle(5, 5, 17, 10),
                    type: TombAreaType.MummyDefendArea,
                    capacity: new CapacityRange(4, 6),
                    allowedMonsters: new HashSet<TombMonsterType>
                    {
                        TombMonsterType.Mummy,
                        TombMonsterType.AssassinBug,
                        TombMonsterType.Spider,
                        TombMonsterType.BruteMummy,
                        TombMonsterType.Skeleton,
                        TombMonsterType.SkeletonMage,
                        TombMonsterType.RoyalSerpent,
                        TombMonsterType.HauntedSkull,
                        TombMonsterType.Ghost,
                        TombMonsterType.Bat,
                        TombMonsterType.Hurricane, TombMonsterType.Crawler, TombMonsterType.Matango,
                    }
                ),
                new(area: new Rectangle(35, 16, 4, 3), type: TombAreaType.DuggyArea),
                new(area: new Rectangle(7, 33, 2, 3), type: TombAreaType.DuggyArea),
                new(area: new Rectangle(10, 31, 2, 2), type: TombAreaType.DuggyArea),
                new(area: new Rectangle(4, 15, 7, 11), type: TombAreaType.SmallArea),
                new(area: new Rectangle(22, 6, 10, 7), type: TombAreaType.SmallArea),
                new(area: new Rectangle(17, 15, 7, 14), type: TombAreaType.SmallArea),
                new(area: new Rectangle(26, 26, 6, 10), type: TombAreaType.SmallArea),
                new(area: new Rectangle(11, 5, 9, 4), type: TombAreaType.MummyArea, capacity: new CapacityRange(1, 1)),
            }
        );

    private static TombLayout CreateLevel19() =>
        new(
            new TombAreaLayout[]
            {
                new(
                    area: new Rectangle(9, 5, 14, 4),
                    capacity: new CapacityRange(1, 3),
                    allowedMonsters: new HashSet<TombMonsterType> { TombMonsterType.Mummy, TombMonsterType.AssassinBug, TombMonsterType.Hurricane, TombMonsterType.Crawler, TombMonsterType.Matango }
                ),
                new(
                    area: new Rectangle(23, 5, 15, 4),
                    capacity: new CapacityRange(1, 3),
                    allowedMonsters: new HashSet<TombMonsterType> { TombMonsterType.Mummy, TombMonsterType.AssassinBug, TombMonsterType.Hurricane, TombMonsterType.Crawler, TombMonsterType.Matango }
                ),
                new(
                    area: new Rectangle(40, 8, 8, 14),
                    capacity: new CapacityRange(2, 4),
                    allowedMonsters: new HashSet<TombMonsterType>
                    {
                        TombMonsterType.Mummy,
                        TombMonsterType.BruteMummy,
                        TombMonsterType.Spider,
                        TombMonsterType.Hurricane, TombMonsterType.Crawler, TombMonsterType.Matango,
                    }
                ),
                new(
                    area: new Rectangle(43, 22, 6, 20),
                    capacity: new CapacityRange(2, 4),
                    allowedMonsters: new HashSet<TombMonsterType>
                    {
                        TombMonsterType.Mummy,
                        TombMonsterType.Spider,
                        TombMonsterType.BruteMummy,
                        TombMonsterType.Hurricane, TombMonsterType.Crawler, TombMonsterType.Matango,
                    }
                ),
                new(
                    area: new Rectangle(25, 44, 15, 5),
                    capacity: new CapacityRange(1, 3),
                    allowedMonsters: new HashSet<TombMonsterType>
                    {
                        TombMonsterType.Mummy,
                        TombMonsterType.Spider,
                        TombMonsterType.BruteMummy,
                        TombMonsterType.Ghost,
                        TombMonsterType.HauntedSkull,
                        TombMonsterType.Hurricane, TombMonsterType.Crawler, TombMonsterType.Matango,
                    }
                ),
                new(
                    area: new Rectangle(2, 21, 4, 18),
                    capacity: new CapacityRange(2, 4),
                    allowedMonsters: new HashSet<TombMonsterType>
                    {
                        TombMonsterType.Mummy,
                        TombMonsterType.Spider,
                        TombMonsterType.BruteMummy,
                        TombMonsterType.Ghost,
                        TombMonsterType.HauntedSkull,
                        TombMonsterType.Hurricane, TombMonsterType.Crawler, TombMonsterType.Matango,
                    }
                ),
                new(
                    area: new Rectangle(6, 14, 9, 9),
                    capacity: new CapacityRange(1, 3),
                    allowedMonsters: new HashSet<TombMonsterType>
                    {
                        TombMonsterType.Mummy,
                        TombMonsterType.Spider,
                        TombMonsterType.BruteMummy,
                        TombMonsterType.Ghost,
                        TombMonsterType.HauntedSkull,
                        TombMonsterType.Hurricane, TombMonsterType.Crawler, TombMonsterType.Matango,
                    }
                ),
                new(
                    area: new Rectangle(27, 14, 10, 6),
                    capacity: new CapacityRange(1, 3),
                    allowedMonsters: new HashSet<TombMonsterType>
                    {
                        TombMonsterType.Mummy,
                        TombMonsterType.AssassinBug,
                        TombMonsterType.Spider,
                        TombMonsterType.BruteMummy,
                        TombMonsterType.Bat,
                        TombMonsterType.Hurricane, TombMonsterType.Crawler, TombMonsterType.Matango,
                    }
                ),
                new(
                    area: new Rectangle(35, 20, 4, 16),
                    capacity: new CapacityRange(1, 3),
                    allowedMonsters: new HashSet<TombMonsterType>
                    {
                        TombMonsterType.Mummy,
                        TombMonsterType.Spider,
                        TombMonsterType.BruteMummy,
                        TombMonsterType.Skeleton,
                        TombMonsterType.Ghost,
                        TombMonsterType.Hurricane, TombMonsterType.Crawler, TombMonsterType.Matango,
                    }
                ),
                new(
                    area: new Rectangle(16, 35, 19, 3),
                    capacity: new CapacityRange(1, 3),
                    allowedMonsters: new HashSet<TombMonsterType>
                    {
                        TombMonsterType.Mummy,
                        TombMonsterType.AssassinBug,
                        TombMonsterType.Spider,
                        TombMonsterType.BruteMummy,
                        TombMonsterType.Bat,
                        TombMonsterType.Hurricane, TombMonsterType.Crawler, TombMonsterType.Matango,
                    }
                ),
                new(
                    area: new Rectangle(11, 24, 5, 12),
                    capacity: new CapacityRange(2, 4),
                    allowedMonsters: new HashSet<TombMonsterType>
                    {
                        TombMonsterType.Mummy,
                        TombMonsterType.AssassinBug,
                        TombMonsterType.Spider,
                        TombMonsterType.BruteMummy,
                        TombMonsterType.Skeleton,
                        TombMonsterType.SkeletonMage,
                        TombMonsterType.RoyalSerpent,
                        TombMonsterType.HauntedSkull,
                        TombMonsterType.Bat,
                        TombMonsterType.Hurricane, TombMonsterType.Crawler, TombMonsterType.Matango,
                    }
                ),
                new(
                    area: new Rectangle(16, 22, 15, 9),
                    type: TombAreaType.MummyDefendArea,
                    capacity: new CapacityRange(5, 6),
                    allowedMonsters: new HashSet<TombMonsterType>
                    {
                        TombMonsterType.Mummy,
                        TombMonsterType.AssassinBug,
                        TombMonsterType.Spider,
                        TombMonsterType.BruteMummy,
                        TombMonsterType.Skeleton,
                        TombMonsterType.SkeletonMage,
                        TombMonsterType.RoyalSerpent,
                        TombMonsterType.HauntedSkull,
                        TombMonsterType.Ghost,
                        TombMonsterType.Bat,
                        TombMonsterType.Hurricane, TombMonsterType.Crawler, TombMonsterType.Matango,
                    }
                ),
                new(area: new Rectangle(11, 31, 3, 2), type: TombAreaType.DuggyArea),
                new(area: new Rectangle(42, 45, 4, 2), type: TombAreaType.DuggyArea),
                new(area: new Rectangle(22, 22, 9, 9), type: TombAreaType.MummyArea),
                new(area: new Rectangle(40, 42, 8, 6), type: TombAreaType.SmallArea),
                new(area: new Rectangle(13, 43, 12, 4), type: TombAreaType.SmallArea),
                new(area: new Rectangle(2, 39, 11, 6), type: TombAreaType.SmallArea),
                new(area: new Rectangle(15, 14, 12, 2), type: TombAreaType.SmallArea),
            }
        );

    private static TombLayout CreateLevel28() =>
        new(
            new TombAreaLayout[]
            {
                new(
                    area: new Rectangle(2, 14, 6, 16),
                    capacity: new CapacityRange(1, 3),
                    allowedMonsters: new HashSet<TombMonsterType> { TombMonsterType.Mummy, TombMonsterType.Spider, TombMonsterType.Hurricane, TombMonsterType.Crawler, TombMonsterType.Matango }
                ),
                new(
                    area: new Rectangle(9, 39, 12, 9),
                    capacity: new CapacityRange(3, 5),
                    allowedMonsters: new HashSet<TombMonsterType>
                    {
                        TombMonsterType.Mummy,
                        TombMonsterType.AssassinBug,
                        TombMonsterType.Spider,
                        TombMonsterType.BruteMummy,
                        TombMonsterType.Bat,
                        TombMonsterType.Hurricane, TombMonsterType.Crawler, TombMonsterType.Matango,
                    }
                ),
                new(
                    area: new Rectangle(42, 37, 11, 10),
                    capacity: new CapacityRange(3, 5),
                    allowedMonsters: new HashSet<TombMonsterType>
                    {
                        TombMonsterType.Mummy,
                        TombMonsterType.AssassinBug,
                        TombMonsterType.Spider,
                        TombMonsterType.BruteMummy,
                        TombMonsterType.Bat,
                        TombMonsterType.Hurricane, TombMonsterType.Crawler, TombMonsterType.Matango,
                    }
                ),
                new(
                    area: new Rectangle(22, 13, 9, 7),
                    capacity: new CapacityRange(1, 3),
                    allowedMonsters: new HashSet<TombMonsterType>
                    {
                        TombMonsterType.Mummy,
                        TombMonsterType.AssassinBug,
                        TombMonsterType.Spider,
                        TombMonsterType.BruteMummy,
                        TombMonsterType.Skeleton,
                        TombMonsterType.RoyalSerpent,
                        TombMonsterType.HauntedSkull,
                        TombMonsterType.Ghost,
                        TombMonsterType.Bat,
                        TombMonsterType.Hurricane, TombMonsterType.Crawler, TombMonsterType.Matango,
                    }
                ),
                new(
                    area: new Rectangle(31, 28, 14, 9),
                    capacity: new CapacityRange(2, 4),
                    allowedMonsters: new HashSet<TombMonsterType>
                    {
                        TombMonsterType.Mummy,
                        TombMonsterType.AssassinBug,
                        TombMonsterType.Spider,
                        TombMonsterType.BruteMummy,
                        TombMonsterType.Skeleton,
                        TombMonsterType.SkeletonMage,
                        TombMonsterType.RoyalSerpent,
                        TombMonsterType.HauntedSkull,
                        TombMonsterType.Hurricane, TombMonsterType.Crawler, TombMonsterType.Matango,
                    }
                ),
                new(
                    area: new Rectangle(31, 15, 13, 5),
                    type: TombAreaType.MummyDefendArea,
                    capacity: new CapacityRange(3, 4),
                    allowedMonsters: new HashSet<TombMonsterType>
                    {
                        TombMonsterType.Mummy,
                        TombMonsterType.AssassinBug,
                        TombMonsterType.Spider,
                        TombMonsterType.BruteMummy,
                        TombMonsterType.Skeleton,
                        TombMonsterType.SkeletonMage,
                        TombMonsterType.RoyalSerpent,
                        TombMonsterType.HauntedSkull,
                        TombMonsterType.Ghost,
                        TombMonsterType.Bat,
                        TombMonsterType.Hurricane, TombMonsterType.Crawler, TombMonsterType.Matango,
                    }
                ),
                new(area: new Rectangle(6, 9, 4, 3), type: TombAreaType.DuggyArea),
                new(area: new Rectangle(28, 30, 2, 4), type: TombAreaType.DuggyArea),
                new(area: new Rectangle(22, 13, 10, 6), type: TombAreaType.MummyArea),
                new(area: new Rectangle(8, 27, 14, 3), type: TombAreaType.SmallArea),
                new(area: new Rectangle(21, 34, 8, 8), type: TombAreaType.SmallArea),
                new(area: new Rectangle(22, 20, 8, 9), type: TombAreaType.SmallArea),
            }
        );

    private static TombLayout CreateLevel39() =>
        new(
            new TombAreaLayout[]
            {
                new(
                    area: new Rectangle(9, 6, 11, 10),
                    capacity: new CapacityRange(3, 5),
                    allowedMonsters: new HashSet<TombMonsterType> { TombMonsterType.Mummy, TombMonsterType.AssassinBug, TombMonsterType.Hurricane, TombMonsterType.Crawler, TombMonsterType.Matango }
                ),
                new(
                    area: new Rectangle(3, 16, 17, 12),
                    capacity: new CapacityRange(4, 5),
                    allowedMonsters: new HashSet<TombMonsterType>
                    {
                        TombMonsterType.Mummy,
                        TombMonsterType.AssassinBug,
                        TombMonsterType.Spider,
                        TombMonsterType.BruteMummy,
                        TombMonsterType.Bat,
                        TombMonsterType.Hurricane, TombMonsterType.Crawler, TombMonsterType.Matango,
                    }
                ),
                new(
                    area: new Rectangle(0, 34, 20, 10),
                    capacity: new CapacityRange(4, 5),
                    allowedMonsters: new HashSet<TombMonsterType>
                    {
                        TombMonsterType.Mummy,
                        TombMonsterType.AssassinBug,
                        TombMonsterType.Spider,
                        TombMonsterType.BruteMummy,
                        TombMonsterType.Skeleton,
                        TombMonsterType.SkeletonMage,
                        TombMonsterType.RoyalSerpent,
                        TombMonsterType.HauntedSkull,
                        TombMonsterType.Hurricane, TombMonsterType.Crawler, TombMonsterType.Matango,
                    }
                ),
                new(
                    area: new Rectangle(1, 50, 18, 9),
                    type: TombAreaType.MummyDefendArea,
                    capacity: new CapacityRange(4, 5),
                    allowedMonsters: new HashSet<TombMonsterType>
                    {
                        TombMonsterType.Mummy,
                        TombMonsterType.AssassinBug,
                        TombMonsterType.Spider,
                        TombMonsterType.BruteMummy,
                        TombMonsterType.Skeleton,
                        TombMonsterType.SkeletonMage,
                        TombMonsterType.RoyalSerpent,
                        TombMonsterType.HauntedSkull,
                        TombMonsterType.Ghost,
                        TombMonsterType.Bat,
                        TombMonsterType.Hurricane, TombMonsterType.Crawler, TombMonsterType.Matango,
                    }
                ),
                new(area: new Rectangle(9, 22, 1, 1), type: TombAreaType.DuggyArea),
                new(area: new Rectangle(8, 40, 1, 1), type: TombAreaType.DuggyArea),
                new(area: new Rectangle(1, 54, 9, 5), type: TombAreaType.MummyArea),
                new(area: new Rectangle(3, 28, 5, 6), type: TombAreaType.SmallArea),
                new(area: new Rectangle(8, 44, 3, 10), type: TombAreaType.SmallArea),
            }
        );

    private static TombLayout CreateLevel77377() =>
        new(
            new TombAreaLayout[]
            {
                new(
                    area: new Rectangle(0, 79, 35, 11),
                    capacity: new CapacityRange(1, 3),
                    allowedMonsters: new HashSet<TombMonsterType> { TombMonsterType.Mummy, TombMonsterType.AssassinBug, TombMonsterType.Hurricane, TombMonsterType.Crawler, TombMonsterType.Matango }
                ),
                new(
                    area: new Rectangle(35, 62, 12, 21),
                    capacity: new CapacityRange(1, 3),
                    allowedMonsters: new HashSet<TombMonsterType>
                    {
                        TombMonsterType.Mummy,
                        TombMonsterType.AssassinBug,
                        TombMonsterType.Spider,
                        TombMonsterType.Hurricane, TombMonsterType.Crawler, TombMonsterType.Matango,
                    }
                ),
                new(
                    area: new Rectangle(0, 58, 25, 16),
                    capacity: new CapacityRange(2, 4),
                    allowedMonsters: new HashSet<TombMonsterType>
                    {
                        TombMonsterType.Mummy,
                        TombMonsterType.Spider,
                        TombMonsterType.BruteMummy,
                        TombMonsterType.Hurricane, TombMonsterType.Crawler, TombMonsterType.Matango,
                    }
                ),
                new(
                    area: new Rectangle(9, 40, 37, 17),
                    capacity: new CapacityRange(1, 3),
                    allowedMonsters: new HashSet<TombMonsterType>
                    {
                        TombMonsterType.Mummy,
                        TombMonsterType.Spider,
                        TombMonsterType.BruteMummy,
                        TombMonsterType.Ghost,
                        TombMonsterType.HauntedSkull,
                        TombMonsterType.Hurricane, TombMonsterType.Crawler, TombMonsterType.Matango,
                    }
                ),
                new(
                    area: new Rectangle(58, 27, 20, 18),
                    capacity: new CapacityRange(3, 5),
                    allowedMonsters: new HashSet<TombMonsterType>
                    {
                        TombMonsterType.Mummy,
                        TombMonsterType.AssassinBug,
                        TombMonsterType.Spider,
                        TombMonsterType.BruteMummy,
                        TombMonsterType.Bat,
                        TombMonsterType.Hurricane, TombMonsterType.Crawler, TombMonsterType.Matango,
                    }
                ),
                new(
                    area: new Rectangle(0, 20, 26, 20),
                    capacity: new CapacityRange(3, 6),
                    allowedMonsters: new HashSet<TombMonsterType>
                    {
                        TombMonsterType.Mummy,
                        TombMonsterType.Spider,
                        TombMonsterType.BruteMummy,
                        TombMonsterType.Skeleton,
                        TombMonsterType.Ghost,
                        TombMonsterType.Hurricane, TombMonsterType.Crawler, TombMonsterType.Matango,
                    }
                ),
                new(
                    area: new Rectangle(46, 0, 34, 24),
                    capacity: new CapacityRange(4, 5),
                    allowedMonsters: new HashSet<TombMonsterType>
                    {
                        TombMonsterType.Mummy,
                        TombMonsterType.AssassinBug,
                        TombMonsterType.Spider,
                        TombMonsterType.BruteMummy,
                        TombMonsterType.Skeleton,
                        TombMonsterType.SkeletonMage,
                        TombMonsterType.RoyalSerpent,
                        TombMonsterType.HauntedSkull,
                        TombMonsterType.Bat,
                        TombMonsterType.Hurricane, TombMonsterType.Crawler, TombMonsterType.Matango,
                    }
                ),
                new(
                    area: new Rectangle(15, 2, 31, 19),
                    type: TombAreaType.MummyDefendArea,
                    capacity: new CapacityRange(4, 5),
                    allowedMonsters: new HashSet<TombMonsterType>
                    {
                        TombMonsterType.Mummy,
                        TombMonsterType.AssassinBug,
                        TombMonsterType.Spider,
                        TombMonsterType.BruteMummy,
                        TombMonsterType.Skeleton,
                        TombMonsterType.SkeletonMage,
                        TombMonsterType.RoyalSerpent,
                        TombMonsterType.HauntedSkull,
                        TombMonsterType.Ghost,
                        TombMonsterType.Bat,
                        TombMonsterType.Hurricane, TombMonsterType.Crawler, TombMonsterType.Matango,
                    }
                ),
                new(area: new Rectangle(66, 31, 5, 4), type: TombAreaType.DuggyArea),
                new(area: new Rectangle(55, 14, 8, 4), type: TombAreaType.DuggyArea),
                new(area: new Rectangle(25, 12, 5, 3), type: TombAreaType.DuggyArea),
                new(area: new Rectangle(31, 11, 4, 3), type: TombAreaType.DuggyArea),
                new(area: new Rectangle(0, 24, 17, 16), type: TombAreaType.MummyArea),
                new(area: new Rectangle(20, 64, 5, 15), type: TombAreaType.SmallArea),
                new(area: new Rectangle(27, 60, 20, 7), type: TombAreaType.SmallArea),
                new(area: new Rectangle(10, 53, 4, 7), type: TombAreaType.SmallArea),
                new(area: new Rectangle(39, 37, 13, 6), type: TombAreaType.SmallArea),
                new(area: new Rectangle(64, 21, 7, 6), type: TombAreaType.SmallArea),
                new(area: new Rectangle(44, 10, 8, 5), type: TombAreaType.SmallArea),
                new(area: new Rectangle(19, 19, 7, 7), type: TombAreaType.SmallArea),
            }
        );
}
