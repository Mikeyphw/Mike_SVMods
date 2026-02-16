using Microsoft.Xna.Framework;
using Mikew.BirdsOfAFeather.Helpers;
using Mikew.BirdsOfAFeather.Helpers;
using StardewValley.Locations;
using StardewValley.Monsters;
using WildEggsHatching.DesertTomb.Configuration;
using WildEggsHatching.DesertTomb.Monsters.Base;
using static WildEggsHatching.EnumsAndStrings.Enums;
using static WildEggsHatching.EnumsAndStrings.Enums.TextOptionValues;

namespace WildEggsHatching.DesertTomb.Model
{
    internal partial class TombArea
    {
        internal Rectangle Area { get; set; }
        internal int MinCapacity { get; set; }
        internal int MaxCapacity { get; set; }
        internal int ChosenCapacity { get; set; }
        internal int CurrentOccupancy { get; set; }
        internal HashSet<TombMonsterType> AllowedMonsters { get; set; }
        internal Dictionary<TombMonsterType, MonsterSpawnStats> MonstersSpawnStats { get; set; }
        internal HashSet<Monster> Monsters { get; set; }
        internal HashSet<Vector2> ValidTiles { get; set; }
        internal Dictionary<TombMonsterType, int> MonsterCounter { get; set; }
        internal TombAreaType Type { get; set; }

        internal TombArea(
            Rectangle area,
            (int Min, int Max)? capacity = null,
            HashSet<TombMonsterType>? allowedMonsters = null,
            TombAreaType areaType = TombAreaType.NormalArea
        )
        {
            Area = area;
            CurrentOccupancy = 0;
            Monsters = new();
            ValidTiles = new();
            Type = areaType;
            (MinCapacity, MaxCapacity) = capacity ?? GetDefaultCapacity(areaType);
            ChosenCapacity = MinCapacity;
            MonsterCounter = EnumExtensions
                .GetValues<TombMonsterType>()
                .ToDictionary(x => x, x => 0);
            AllowedMonsters = allowedMonsters ?? GetDefaultAllowedMonsters(areaType);
            if (
                areaType != TombAreaType.DuggyArea
                && AllowedMonsters.Contains(TombMonsterType.Duggy)
            )
            {
                AllowedMonsters.Remove(TombMonsterType.Duggy);
            }
            MonstersSpawnStats = new();
        }

        internal Dictionary<TombMonsterType, MonsterSpawnStats> GetMonsterSpawnStatsByAreaType(
            SpawnConfiguration? spawnConfiguration
        )
        {
            // Use configuration if provided, otherwise fallback to defaults
            if (
                spawnConfiguration != null
                && spawnConfiguration.MonsterSpawnRatesByArea.ContainsKey(Type)
            )
            {
                return spawnConfiguration.MonsterSpawnRatesByArea[Type];
            }

            // Fallback to hardcoded defaults if no config provided
            var monsterSpawnStats = new Dictionary<
                TombAreaType,
                Dictionary<TombMonsterType, MonsterSpawnStats>
            >()
            {
                [TombAreaType.MummyArea] = new Dictionary<TombMonsterType, MonsterSpawnStats>
                {
                    [TombMonsterType.TheTombPharaoh] = new(1, 2, 0),
                },
                [TombAreaType.MummyDefendArea] = new Dictionary<TombMonsterType, MonsterSpawnStats>
                {
                    [TombMonsterType.Mummy] = new(3, 1, 0),
                    [TombMonsterType.HauntedSkull] = new(1, 1, 0),
                    [TombMonsterType.Ghost] = new(1, 0, 0),
                    [TombMonsterType.RoyalSerpent] = new(1, 0, 0),
                    [TombMonsterType.BruteMummy] = new(2, 1, 0),
                    [TombMonsterType.Skeleton] = new(1, 1, 0),
                    [TombMonsterType.SkeletonMage] = new(2, 1, 0),
                },
                [TombAreaType.NormalArea] = new Dictionary<TombMonsterType, MonsterSpawnStats>
                {
                    [TombMonsterType.Mummy] = new(99, 1, 0),
                    [TombMonsterType.HauntedSkull] = new(1, 1, 0),
                    [TombMonsterType.Ghost] = new(1, 0, 0),
                    [TombMonsterType.AssassinBug] = new(1, 0, 0),
                    [TombMonsterType.Bat] = new(1, 0, 0),
                    [TombMonsterType.RoyalSerpent] = new(2, 0, 0),
                    [TombMonsterType.BruteMummy] = new(99, 1, 0),
                    [TombMonsterType.Skeleton] = new(99, 1, 0),
                    [TombMonsterType.SkeletonMage] = new(1, 1, 0),
                    [TombMonsterType.Spider] = new(2, 0, 0),
                },
                [TombAreaType.SmallArea] = new Dictionary<TombMonsterType, MonsterSpawnStats>
                {
                    [TombMonsterType.Mummy] = new(2, 1, 0),
                    [TombMonsterType.AssassinBug] = new(1, 0, 0),
                },
                [TombAreaType.DuggyArea] = new Dictionary<TombMonsterType, MonsterSpawnStats>
                {
                    [TombMonsterType.Duggy] = new(4, 0, 0),
                },
            };

            return monsterSpawnStats[Type];
        }

        internal void Reset()
        {
            CurrentOccupancy = 0;
            Monsters = new();
            ValidTiles = new();
            ChosenCapacity = MinCapacity;
            MonsterCounter = EnumExtensions
                .GetValues<TombMonsterType>()
                .ToDictionary(x => x, x => 0);
        }

        private (int, int) GetDefaultCapacity(TombAreaType areaType)
        {
            return areaType switch
            {
                TombAreaType.DuggyArea or TombAreaType.SmallArea => (1, 2),
                _ => (1, 1),
            };
        }

        private HashSet<TombMonsterType> GetDefaultAllowedMonsters(TombAreaType areaType)
        {
            return areaType switch
            {
                TombAreaType.DuggyArea => new HashSet<TombMonsterType> { TombMonsterType.Duggy },
                TombAreaType.MummyArea => new HashSet<TombMonsterType>
                {
                    TombMonsterType.TheTombPharaoh,
                },
                TombAreaType.SmallArea => new HashSet<TombMonsterType>
                {
                    TombMonsterType.Mummy,
                    TombMonsterType.AssassinBug,
                    TombMonsterType.Spider,
                },
                _ => new(),
            };
        }

        internal void UpdateAllowedMonstersAndTiles(
            HashSet<TombMonsterType> monsterTypes,
            SpawnConfiguration? spawnConfiguration,
            MineShaft mineShaft
        )
        {
            MonstersSpawnStats = GetMonsterSpawnStatsByAreaType(spawnConfiguration);
            if (!ModEntry.Config.EnableSpiders)
            {
                MonstersSpawnStats.Remove(TombMonsterType.Spider);
            }
            MonstersSpawnStats.RemoveWhere(m => IsMonsterAllowedForDifficulty(m.Key, monsterTypes));
            AllowedMonsters = MonstersSpawnStats.Keys.ToHashSet();
            ValidTiles = TileUtility.GetTombAreaValidTiles(Area, Type, mineShaft);
        }

        /// <summary>
        /// Validate if a monster is allowed for the given difficulty
        /// </summary>
        private bool IsMonsterAllowedForDifficulty(
            TombMonsterType monsterType,
            HashSet<TombMonsterType> monsterTypes
        )
        {
            // Always allow TheTombPharaoh in MummyArea regardless of difficulty
            bool isMummyAreaSpecialCase =
                Type == TombAreaType.MummyArea && monsterType == TombMonsterType.TheTombPharaoh;
            if (isMummyAreaSpecialCase)
            {
                return true;
            }
            return monsterTypes.Contains(monsterType);
        }
    }
}
