using StardewValley.Locations;
using WildEggsHatching.DesertTomb.Configuration;
using WildEggsHatching.DesertTomb.TombMonsters.Base;
using static WildEggsHatching.EnumsAndStrings.Enums;
using static WildEggsHatching.EnumsAndStrings.Enums.TextOptionValues;

namespace WildEggsHatching.DesertTomb.Core
{
    internal class TombHandler
    {
        internal Dictionary<Assets.MineLevel, Tomb> Tombs;

        internal TombHandler(
            MineShaft mineShaft,
            HashSet<TombMonsterType> tombMonsters,
            SpawnConfiguration? spawnConfiguration = null
        )
        {
            Tombs = GenerateTombs();
            foreach (var area in Tombs.Values.SelectMany(t => t.LevelAreas))
            {
                area.UpdateAllowedMonstersAndTiles(tombMonsters, spawnConfiguration, mineShaft);
            }
        }

        private Dictionary<Assets.MineLevel, Tomb> GenerateTombs()
        {
            return new Dictionary<Assets.MineLevel, Tomb>
            {
                { Assets.MineLevel.Level14, CreateLevel14Tomb() },
                { Assets.MineLevel.Level19, CreateLevel19Tomb() },
                { Assets.MineLevel.Level28, CreateLevel28Tomb() },
                { Assets.MineLevel.Level39, CreateLevel39Tomb() },
                { Assets.MineLevel.Level77377, CreateLevel77377Tomb() },
            };
        }

        private Tomb CreateLevel14Tomb()
        {
            var tomb = new Tomb()
            {
                LevelAreas = new TombArea[]
                {
                    new(
                        area: new(32, 5, 14, 15),
                        capacity: (4, 6),
                        allowedMonsters: new() { TombMonsterType.Mummy, TombMonsterType.Spider }
                    ),
                    new(
                        area: new(32, 20, 14, 18),
                        capacity: (4, 6),
                        allowedMonsters: new()
                        {
                            TombMonsterType.Mummy,
                            TombMonsterType.AssassinBug,
                            TombMonsterType.Bat,
                        }
                    ),
                    new(
                        area: new(2, 26, 15, 11),
                        capacity: (1, 3),
                        allowedMonsters: new()
                        {
                            TombMonsterType.Mummy,
                            TombMonsterType.AssassinBug,
                            TombMonsterType.Spider,
                            TombMonsterType.BruteMummy,
                            TombMonsterType.Skeleton,
                            TombMonsterType.SkeletonMage,
                            TombMonsterType.RoyalSerpent,
                            TombMonsterType.HauntedSkull,
                        }
                    ),
                    new(
                        area: new(5, 5, 17, 10),
                        capacity: (4, 6),
                        allowedMonsters: new()
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
                        },
                        areaType: TombAreaType.MummyDefendArea
                    ),
                    new(area: new(35, 16, 4, 3), areaType: TombAreaType.DuggyArea),
                    new(area: new(7, 33, 2, 3), areaType: TombAreaType.DuggyArea),
                    new(area: new(10, 31, 2, 2), areaType: TombAreaType.DuggyArea),
                    new(area: new(4, 15, 7, 11), areaType: TombAreaType.SmallArea),
                    new(area: new(22, 6, 10, 7), areaType: TombAreaType.SmallArea),
                    new(area: new(17, 15, 7, 14), areaType: TombAreaType.SmallArea),
                    new(area: new(26, 26, 6, 10), areaType: TombAreaType.SmallArea),
                    new(area: new(11, 5, 9, 4), capacity: (1, 1), areaType: TombAreaType.MummyArea),
                },
            };
            return tomb;
        }

        private Tomb CreateLevel19Tomb()
        {
            var tomb = new Tomb()
            {
                LevelAreas = new TombArea[]
                {
                    new(
                        area: new(9, 5, 14, 4),
                        capacity: (1, 3),
                        allowedMonsters: new()
                        {
                            TombMonsterType.Mummy,
                            TombMonsterType.AssassinBug,
                        }
                    ),
                    new(
                        area: new(23, 5, 15, 4),
                        capacity: (1, 3),
                        allowedMonsters: new()
                        {
                            TombMonsterType.Mummy,
                            TombMonsterType.AssassinBug,
                        }
                    ),
                    new(
                        area: new(40, 8, 8, 14),
                        capacity: (2, 4),
                        allowedMonsters: new()
                        {
                            TombMonsterType.Mummy,
                            TombMonsterType.BruteMummy,
                            TombMonsterType.Spider,
                        }
                    ),
                    new(
                        area: new(43, 22, 6, 20),
                        capacity: (2, 4),
                        allowedMonsters: new()
                        {
                            TombMonsterType.Mummy,
                            TombMonsterType.Spider,
                            TombMonsterType.BruteMummy,
                        }
                    ),
                    new(
                        area: new(25, 44, 15, 5),
                        capacity: (1, 3),
                        allowedMonsters: new()
                        {
                            TombMonsterType.Mummy,
                            TombMonsterType.Spider,
                            TombMonsterType.BruteMummy,
                            TombMonsterType.Ghost,
                            TombMonsterType.HauntedSkull,
                        }
                    ),
                    new(
                        area: new(2, 21, 4, 18),
                        capacity: (2, 4),
                        allowedMonsters: new()
                        {
                            TombMonsterType.Mummy,
                            TombMonsterType.Spider,
                            TombMonsterType.BruteMummy,
                            TombMonsterType.Ghost,
                            TombMonsterType.HauntedSkull,
                        }
                    ),
                    new(
                        area: new(6, 14, 9, 9),
                        capacity: (1, 3),
                        allowedMonsters: new()
                        {
                            TombMonsterType.Mummy,
                            TombMonsterType.Spider,
                            TombMonsterType.BruteMummy,
                            TombMonsterType.Ghost,
                            TombMonsterType.HauntedSkull,
                        }
                    ),
                    new(
                        area: new(27, 14, 10, 6),
                        capacity: (1, 3),
                        allowedMonsters: new()
                        {
                            TombMonsterType.Mummy,
                            TombMonsterType.AssassinBug,
                            TombMonsterType.Spider,
                            TombMonsterType.BruteMummy,
                            TombMonsterType.Bat,
                        }
                    ),
                    new(
                        area: new(35, 20, 4, 16),
                        capacity: (1, 3),
                        allowedMonsters: new()
                        {
                            TombMonsterType.Mummy,
                            TombMonsterType.Spider,
                            TombMonsterType.BruteMummy,
                            TombMonsterType.Skeleton,
                            TombMonsterType.Ghost,
                        }
                    ),
                    new(
                        area: new(16, 35, 19, 3),
                        capacity: (1, 3),
                        allowedMonsters: new()
                        {
                            TombMonsterType.Mummy,
                            TombMonsterType.AssassinBug,
                            TombMonsterType.Spider,
                            TombMonsterType.BruteMummy,
                            TombMonsterType.Bat,
                        }
                    ),
                    new(
                        area: new(11, 24, 5, 12),
                        capacity: (2, 4),
                        allowedMonsters: new()
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
                        }
                    ),
                    new(
                        area: new(16, 22, 15, 9),
                        capacity: (5, 6),
                        allowedMonsters: new()
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
                        },
                        areaType: TombAreaType.MummyDefendArea
                    ),
                    new(area: new(11, 31, 3, 2), areaType: TombAreaType.DuggyArea),
                    new(area: new(42, 45, 4, 2), areaType: TombAreaType.DuggyArea),
                    new(area: new(22, 22, 9, 9), areaType: TombAreaType.MummyArea),
                    new(area: new(40, 42, 8, 6), areaType: TombAreaType.SmallArea),
                    new(area: new(13, 43, 12, 4), areaType: TombAreaType.SmallArea),
                    new(area: new(2, 39, 11, 6), areaType: TombAreaType.SmallArea),
                    new(area: new(15, 14, 12, 2), areaType: TombAreaType.SmallArea),
                },
            };
            return tomb;
        }

        private Tomb CreateLevel28Tomb()
        {
            var tomb = new Tomb()
            {
                LevelAreas = new TombArea[]
                {
                    new(
                        area: new(2, 14, 6, 16),
                        capacity: (1, 3),
                        allowedMonsters: new() { TombMonsterType.Mummy, TombMonsterType.Spider }
                    ),
                    new(
                        area: new(9, 39, 12, 9),
                        capacity: (3, 5),
                        allowedMonsters: new()
                        {
                            TombMonsterType.Mummy,
                            TombMonsterType.AssassinBug,
                            TombMonsterType.Spider,
                            TombMonsterType.BruteMummy,
                            TombMonsterType.Bat,
                        }
                    ),
                    new(
                        area: new(42, 37, 11, 10),
                        capacity: (3, 5),
                        allowedMonsters: new()
                        {
                            TombMonsterType.Mummy,
                            TombMonsterType.AssassinBug,
                            TombMonsterType.Spider,
                            TombMonsterType.BruteMummy,
                            TombMonsterType.Bat,
                        }
                    ),
                    new(
                        area: new(22, 13, 9, 7),
                        capacity: (1, 3),
                        allowedMonsters: new()
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
                        }
                    ),
                    new(
                        area: new(31, 28, 14, 9),
                        capacity: (2, 4),
                        allowedMonsters: new()
                        {
                            TombMonsterType.Mummy,
                            TombMonsterType.AssassinBug,
                            TombMonsterType.Spider,
                            TombMonsterType.BruteMummy,
                            TombMonsterType.Skeleton,
                            TombMonsterType.SkeletonMage,
                            TombMonsterType.RoyalSerpent,
                            TombMonsterType.HauntedSkull,
                        }
                    ),
                    new(
                        area: new(31, 15, 13, 5),
                        capacity: (3, 4),
                        allowedMonsters: new()
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
                        },
                        areaType: TombAreaType.MummyDefendArea
                    ),
                    new(area: new(6, 9, 4, 3), areaType: TombAreaType.DuggyArea),
                    new(area: new(28, 30, 2, 4), areaType: TombAreaType.DuggyArea),
                    new(area: new(22, 13, 10, 6), areaType: TombAreaType.MummyArea),
                    new(area: new(8, 27, 14, 3), areaType: TombAreaType.SmallArea),
                    new(area: new(21, 34, 8, 8), areaType: TombAreaType.SmallArea),
                    new(area: new(22, 20, 8, 9), areaType: TombAreaType.SmallArea),
                },
            };
            return tomb;
        }

        private Tomb CreateLevel39Tomb()
        {
            var tomb = new Tomb()
            {
                LevelAreas = new TombArea[]
                {
                    new(
                        area: new(9, 6, 11, 10),
                        capacity: (3, 5),
                        allowedMonsters: new()
                        {
                            TombMonsterType.Mummy,
                            TombMonsterType.AssassinBug,
                        }
                    ),
                    new(
                        area: new(3, 16, 17, 12),
                        capacity: (4, 5),
                        allowedMonsters: new()
                        {
                            TombMonsterType.Mummy,
                            TombMonsterType.AssassinBug,
                            TombMonsterType.Spider,
                            TombMonsterType.BruteMummy,
                            TombMonsterType.Bat,
                        }
                    ),
                    new(
                        area: new(0, 34, 20, 10),
                        capacity: (4, 5),
                        allowedMonsters: new()
                        {
                            TombMonsterType.Mummy,
                            TombMonsterType.AssassinBug,
                            TombMonsterType.Spider,
                            TombMonsterType.BruteMummy,
                            TombMonsterType.Skeleton,
                            TombMonsterType.SkeletonMage,
                            TombMonsterType.RoyalSerpent,
                            TombMonsterType.HauntedSkull,
                        }
                    ),
                    new(
                        area: new(1, 50, 18, 9),
                        capacity: (4, 5),
                        allowedMonsters: new()
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
                        },
                        areaType: TombAreaType.MummyDefendArea
                    ),
                    new(area: new(9, 22, 1, 1), areaType: TombAreaType.DuggyArea),
                    new(area: new(8, 40, 1, 1), areaType: TombAreaType.DuggyArea),
                    new(area: new(1, 54, 9, 5), areaType: TombAreaType.MummyArea),
                    new(area: new(3, 28, 5, 6), areaType: TombAreaType.SmallArea),
                    new(area: new(8, 44, 3, 10), areaType: TombAreaType.SmallArea),
                },
            };
            return tomb;
        }

        private Tomb CreateLevel77377Tomb()
        {
            var tomb = new Tomb()
            {
                LevelAreas = new TombArea[]
                {
                    new(
                        area: new(0, 79, 35, 11),
                        capacity: (1, 3),
                        allowedMonsters: new()
                        {
                            TombMonsterType.Mummy,
                            TombMonsterType.AssassinBug,
                        }
                    ),
                    new(
                        area: new(35, 62, 12, 21),
                        capacity: (1, 3),
                        allowedMonsters: new()
                        {
                            TombMonsterType.Mummy,
                            TombMonsterType.AssassinBug,
                            TombMonsterType.Spider,
                        }
                    ),
                    new(
                        area: new(0, 58, 25, 16),
                        capacity: (2, 4),
                        allowedMonsters: new()
                        {
                            TombMonsterType.Mummy,
                            TombMonsterType.Spider,
                            TombMonsterType.BruteMummy,
                        }
                    ),
                    new(
                        area: new(9, 40, 37, 17),
                        capacity: (1, 3),
                        allowedMonsters: new()
                        {
                            TombMonsterType.Mummy,
                            TombMonsterType.Spider,
                            TombMonsterType.BruteMummy,
                            TombMonsterType.Ghost,
                            TombMonsterType.HauntedSkull,
                        }
                    ),
                    new(
                        area: new(58, 27, 20, 18),
                        capacity: (3, 5),
                        allowedMonsters: new()
                        {
                            TombMonsterType.Mummy,
                            TombMonsterType.AssassinBug,
                            TombMonsterType.Spider,
                            TombMonsterType.BruteMummy,
                            TombMonsterType.Bat,
                        }
                    ),
                    new(
                        area: new(0, 20, 26, 20),
                        capacity: (3, 6),
                        allowedMonsters: new()
                        {
                            TombMonsterType.Mummy,
                            TombMonsterType.Spider,
                            TombMonsterType.BruteMummy,
                            TombMonsterType.Skeleton,
                            TombMonsterType.Ghost,
                        }
                    ),
                    new(
                        area: new(46, 0, 34, 24),
                        capacity: (4, 5),
                        allowedMonsters: new()
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
                        }
                    ),
                    new(
                        area: new(15, 2, 31, 19),
                        capacity: (4, 5),
                        allowedMonsters: new()
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
                        },
                        areaType: TombAreaType.MummyDefendArea
                    ),
                    new(area: new(66, 31, 5, 4), areaType: TombAreaType.DuggyArea),
                    new(area: new(55, 14, 8, 4), areaType: TombAreaType.DuggyArea),
                    new(area: new(25, 12, 5, 3), areaType: TombAreaType.DuggyArea),
                    new(area: new(31, 11, 4, 3), areaType: TombAreaType.DuggyArea),
                    new(area: new(0, 24, 17, 16), areaType: TombAreaType.MummyArea),
                    new(area: new(20, 64, 5, 15), areaType: TombAreaType.SmallArea),
                    new(area: new(27, 60, 20, 7), areaType: TombAreaType.SmallArea),
                    new(area: new(10, 53, 4, 7), areaType: TombAreaType.SmallArea),
                    new(area: new(39, 37, 13, 6), areaType: TombAreaType.SmallArea),
                    new(area: new(64, 21, 7, 6), areaType: TombAreaType.SmallArea),
                    new(area: new(44, 10, 8, 5), areaType: TombAreaType.SmallArea),
                    new(area: new(19, 19, 7, 7), areaType: TombAreaType.SmallArea),
                },
            };
            return tomb;
        }
    }
}
