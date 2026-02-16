using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Mikew.BirdsOfAFeather.Helpers;
using StardewModdingAPI;
using StardewValley;
using Mikew.BirdsOfAFeather.Helpers;
using StardewValley.Locations;
using StardewValley.Monsters;
using WildEggsHatching.DesertTomb.Configuration;
using WildEggsHatching.DesertTomb.Model;
using WildEggsHatching.DesertTomb.Monsters.Base;
using WildEggsHatching.DesertTomb.Monsters.Creatures;
using static WildEggsHatching.EnumsAndStrings.Enums;
using static WildEggsHatching.EnumsAndStrings.Enums.Assets;
using static WildEggsHatching.EnumsAndStrings.Enums.TextOptionValues;
using static WildEggsHatching.ModEntry;

namespace WildEggsHatching.DesertTomb.Spawning
{
    /// <summary>
    /// Centralized calculation engine for spawn rates and multipliers
    /// </summary>
    public class SpawnRateCalculator
    {
        private HashSet<TombMonsterType> AllowedMonstersByDifficuty;
        private readonly SpawnConfiguration _config;
        private readonly MonsterMultipliers _monsterMultipliers;
        private readonly MineShaft _mineShaft;
        private readonly TombArea[] TombAreas;
        private readonly TombHandler TombHandler;
        private readonly TombDifficulty _tombDifficulty;
        private readonly Dictionary<TombMonsterType, int> _maxMonsterInTomb;

        private Dictionary<
            TombMonsterType,
            Func<Vector2, int, int, string, Monster>
        > MonsterFactory { get; set; }
        private Dictionary<TombMonsterType, int> TombMonsterCounter { get; }
        private readonly Dictionary<TombMonsterType, string> TexturesNames = new()
        {
            { TombMonsterType.AssassinBug, "Assassin Bug" },
            { TombMonsterType.Bat, "Bat_dangerous" },
            { TombMonsterType.BruteMummy, "Brute Mummy" },
            { TombMonsterType.Duggy, "Sandy Duggy" },
            { TombMonsterType.HauntedSkull, "Fly" },
            { TombMonsterType.Ghost, "Carbon Ghost" },
            { TombMonsterType.Mummy, "Mummy" },
            { TombMonsterType.RoyalSerpent, "Royal Serpent" },
            { TombMonsterType.Skeleton, "Skeleton" },
            { TombMonsterType.SkeletonMage, "Skeleton Mage" },
            { TombMonsterType.Spider, "Spider" },
            { TombMonsterType.TheTombPharaoh, "Mummy_dangerous" },
        };

        public SpawnRateCalculator(
            SpawnConfiguration config,
            MineShaft mineShaft,
            MineLevel mineLevel,
            TombDifficulty difficulty
        )
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _tombDifficulty = difficulty;
            _mineShaft = mineShaft;
            _monsterMultipliers = _config.Multipliers;
            MonsterFactory = GetMonsterFactory();
            _maxMonsterInTomb = _config.Global.MaxMonsterInTomb;
            TombMonsterCounter = EnumExtensions
                .GetValues<TombMonsterType>()
                .ToDictionary(x => x, y => 0);

            AllowedMonstersByDifficuty = GetAllowedMonstersForDifficulty();
            TombHandler = new(mineShaft, AllowedMonstersByDifficuty, config);
            TombAreas = TombHandler.Tombs[mineLevel].LevelAreas;
        }

        private HashSet<TombMonsterType> GetAllowedMonstersForDifficulty(
            SpawnConfiguration? config = null
        )
        {
            if (config != null)
            {
                return config.AllowedMonstersByDifficulty[_tombDifficulty];
            }

            // Fallback to hardcoded defaults
            var easyMonsters = new HashSet<TombMonsterType>
            {
                TombMonsterType.Bat,
                TombMonsterType.Mummy,
                TombMonsterType.Ghost,
                TombMonsterType.Duggy,
                TombMonsterType.TheTombPharaoh,
            };
            var moderateMonsters = easyMonsters
                .Union(
                    new HashSet<TombMonsterType>
                    {
                        TombMonsterType.AssassinBug,
                        TombMonsterType.Spider,
                        TombMonsterType.BruteMummy,
                        TombMonsterType.Skeleton,
                        TombMonsterType.SkeletonMage,
                    }
                )
                .ToHashSet();
            var hardMonsters = moderateMonsters
                .Union(
                    new HashSet<TombMonsterType>
                    {
                        TombMonsterType.RoyalSerpent,
                        TombMonsterType.HauntedSkull,
                    }
                )
                .ToHashSet();
            return _tombDifficulty switch
            {
                TombDifficulty.Easy => easyMonsters,
                TombDifficulty.Moderate => moderateMonsters,
                TombDifficulty.Hard => hardMonsters,
                _ => easyMonsters,
            };
        }

        /// <summary>
        /// Determines which monsters can spawn as guards in a specific area
        /// </summary>
        internal List<TombMonsterType> GetAllowedGuardingMonsters(TombArea area)
        {
            var mummies = new List<TombMonsterType>
            {
                TombMonsterType.Mummy,
                TombMonsterType.BruteMummy,
                TombMonsterType.Skeleton,
            };
            var availableMummies = area.MonstersSpawnStats.Keys.Intersect(mummies).ToList();
            var allowedMonsters = area.AllowedMonsters.Where(m => !IsWanderer(m)).ToList();
            return area.Type != TombAreaType.DuggyArea
                ? allowedMonsters.Union(availableMummies).Distinct().ToList()
                : allowedMonsters;
        }

        /// <summary>
        /// Checks if a monster type is within spawn limits
        /// </summary>
        internal bool IsWithinMaxLimit(TombMonsterType monsterType, TombArea area)
        {
            if (area.MonsterCounter[monsterType] >= area.MonstersSpawnStats[monsterType].Max)
            {
                return false;
            }
            if (TombMonsterCounter[monsterType] >= _maxMonsterInTomb[monsterType])
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Determines if a monster type is a wanderer
        /// </summary>
        public bool IsWanderer(TombMonsterType monsterType)
        {
            return monsterType switch
            {
                TombMonsterType.Bat
                or TombMonsterType.HauntedSkull
                or TombMonsterType.Ghost
                or TombMonsterType.RoyalSerpent => true,
                _ => false,
            };
        }

        #region
        internal void SpawnMonsters()
        {
            _mineShaft.characters.RemoveWhere(c => c is Monster && !c.Name.StartsWith("Tomb"));
            AddGuardingMonsters();
            AddWanderingMonsters();
        }

        private Dictionary<
            TombMonsterType,
            Func<Vector2, int, int, string, Monster>
        > GetMonsterFactory()
        {
            return new()
            {
                {
                    TombMonsterType.TheTombPharaoh,
                    (position, difficultyIncreament, speed, textureName) =>
                        (Monster)
                            new ThePharaoh(
                                monsterMultipliers: _monsterMultipliers,
                                position: position,
                                difficulty: difficultyIncreament,
                                textureName: textureName,
                                speedToAdd: speed
                            )
                },
                {
                    TombMonsterType.Ghost,
                    (position, difficultyIncreament, speed, textureName) =>
                        new TombGhost(
                            monsterMultipliers: _monsterMultipliers,
                            position: position,
                            difficulty: difficultyIncreament,
                            textureName: textureName,
                            speedToAdd: speed
                        )
                },
                {
                    TombMonsterType.Bat,
                    (position, difficultyIncreament, speed, textureName) =>
                        new TombBat(
                            monsterMultipliers: _monsterMultipliers,
                            position: position,
                            121,
                            difficulty: difficultyIncreament,
                            speedToAdd: speed,
                            textureName: textureName
                        )
                },
                {
                    TombMonsterType.Duggy,
                    (position, difficultyIncreament, speed, textureName) =>
                        new TombDuggy(
                            monsterMultipliers: _monsterMultipliers,
                            position: position,
                            difficulty: difficultyIncreament,
                            speedToAdd: speed,
                            textureName: textureName
                        )
                },
                {
                    TombMonsterType.HauntedSkull,
                    (position, difficultyIncreament, speed, textureName) =>
                        new TombHauntedSkull(
                            monsterMultipliers: _monsterMultipliers,
                            position: position,
                            difficulty: difficultyIncreament,
                            speedToAdd: speed,
                            textureName: textureName
                        )
                },
                {
                    TombMonsterType.Spider,
                    (position, difficultyIncreament, speed, textureName) =>
                        new TombSpider(
                            monsterMultipliers: _monsterMultipliers,
                            position: position,
                            difficulty: difficultyIncreament,
                            speedToAdd: speed,
                            textureName: textureName
                        )
                },
                {
                    TombMonsterType.AssassinBug,
                    (position, difficultyIncreament, speed, textureName) =>
                        new TombAssassinBug(
                            monsterMultipliers: _monsterMultipliers,
                            position: position,
                            _mineShaft.mineRandom.Next(4),
                            textureName: textureName,
                            difficulty: difficultyIncreament,
                            speedToAdd: speed
                        )
                },
                {
                    TombMonsterType.RoyalSerpent,
                    (position, difficultyIncreament, speed, textureName) =>
                        new TombSerpent(
                            monsterMultipliers: _monsterMultipliers,
                            position: position,
                            difficulty: difficultyIncreament,
                            textureName: textureName,
                            speedToAdd: speed
                        )
                },
                {
                    TombMonsterType.BruteMummy,
                    (position, difficultyIncreament, speed, textureName) =>
                        new BruteMummy(
                            monsterMultipliers: _monsterMultipliers,
                            position: position,
                            difficulty: difficultyIncreament,
                            speedToAdd: speed,
                            textureName: textureName
                        )
                },
                {
                    TombMonsterType.Mummy,
                    (position, difficultyIncreament, speed, textureName) =>
                        new TombMummy(
                            monsterMultipliers: _monsterMultipliers,
                            position: position,
                            difficulty: difficultyIncreament,
                            speedToAdd: speed,
                            textureName: textureName
                        )
                },
                {
                    TombMonsterType.Skeleton,
                    (position, difficultyIncreament, speed, textureName) =>
                        new TombSkeleton(
                            monsterMultipliers: _monsterMultipliers,
                            position: position,
                            difficulty: difficultyIncreament,
                            speedToAdd: speed,
                            textureName: textureName
                        )
                },
                {
                    TombMonsterType.SkeletonMage,
                    (position, difficultyIncreament, speed, textureName) =>
                        new TombSkeletonMage(
                            monsterMultipliers: _monsterMultipliers,
                            position: position,
                            difficulty: difficultyIncreament,
                            speedToAdd: speed,
                            textureName: textureName
                        )
                },
            };
        }

        /// <summary>
        /// Adds guarding monsters to each tomb area based on area-specific configurations and spawn limits.
        /// This method populates tomb areas with stationary monsters that guard specific locations.
        /// </summary>
        /// <remarks>
        /// The method processes each tomb area by:
        /// 1. Determining the area's monster capacity (random between min/max values)
        /// 2. Identifying which monsters are allowed to spawn as guards in this area
        /// 3. Attempting to spawn monsters until capacity is reached or no valid tiles remain
        /// 4. Respecting global and per-area monster limits defined in configuration
        /// </remarks>
        private void AddGuardingMonsters()
        {
            // Process each tomb area to populate with guarding monsters
            for (int i = 0; i < TombAreas.Length; i++)
            {
                var area = TombAreas[i];
                var monsterSpawnStats = area.MonstersSpawnStats;

                // Randomly determine how many monsters this area should contain
                // Uses area-specific min/max capacity values from configuration
                area.ChosenCapacity = _mineShaft.mineRandom.Next(
                    area.MinCapacity,
                    area.MaxCapacity + 1
                );

                // Get list of monster types that can spawn as guards in this area
                var allowedGuardingMonsters = GetAllowedGuardingMonsters(area);

                // Attempt to spawn monsters until area reaches capacity or runs out of valid tiles
                // Uses a maximum try count to prevent infinite loops
                const int maxTries = 1000;
                int tryCount;
                for (
                    tryCount = 0;
                    tryCount <= maxTries
                        && area.CurrentOccupancy < area.ChosenCapacity
                        && area.ValidTiles.Any();
                    tryCount++
                )
                {
                    // Randomize the order of monster types to ensure variety
                    allowedGuardingMonsters = allowedGuardingMonsters
                        .OrderBy(x => Game1.random.Next())
                        .ToList();

                    // Try to spawn each allowed monster type
                    for (int j = 0; j < allowedGuardingMonsters.Count; j++)
                    {
                        TombMonsterType monsterType = allowedGuardingMonsters[j];

                        // Special handling for SkeletonMage - only 25% chance to spawn outside MummyArea
                        // This prevents SkeletonMage from being too common in non-mummy areas
                        if (
                            monsterType == TombMonsterType.SkeletonMage
                            && area.Type != TombAreaType.MummyArea
                        )
                        {
                            monsterType =
                                _mineShaft.mineRandom.NextDouble() < 0.25
                                    ? monsterType
                                    : TombMonsterType.Skeleton;
                        }

                        // If no allowed monsters are within limits, default to Mummy
                        // This ensures something always spawns even when limits are exceeded
                        if (!allowedGuardingMonsters.Any(m => IsWithinMaxLimit(m, area)))
                        {
                            monsterType = TombMonsterType.Mummy;
                        }
                        // Skip this monster type if it exceeds area or global limits
                        else if (!IfIsWithinMaxLimit(monsterType, area))
                        {
                            continue;
                        }

                        // Stop spawning if area has reached capacity
                        if (area.CurrentOccupancy >= area.ChosenCapacity)
                        {
                            break;
                        }

                        // Attempt to spawn the monster in a valid location
                        var monsterSpawned = AddMonstersToArea(
                            area,
                            monsterSpawnStats,
                            monsterType
                        );

                        // Update counters if spawn was successful
                        if (monsterSpawned)
                        {
                            TombMonsterCounter[monsterType]++;
                            area.CurrentOccupancy++;
                            area.MonsterCounter[monsterType]++;
                        }
                        else
                        {
                            LoggerHelper.VerboseLog($"Couldn't spawn monster.");
                        }
                    }
                }
            }
        }

        private bool AddMonstersToArea(
            TombArea area,
            Dictionary<TombMonsterType, MonsterSpawnStats> monsterSpawnStats,
            TombMonsterType monsterType
        )
        {
            if (MonsterFactory.TryGetValue(monsterType, out var factory))
            {
                if (monsterSpawnStats.TryGetValue(monsterType, out var spawnStat))
                {
                    var spawned = AddMonsters(
                        position =>
                            factory(
                                position,
                                spawnStat.Difficulty,
                                spawnStat.Speed,
                                TexturesNames[monsterType]
                            ),
                        area
                    );
                    if (!spawned)
                    {
                        LoggerHelper.VerboseLog(
                            $"Couldn't spawn monster {monsterType}. Area: {area.Area}",
                            LogLevel.Warn
                        );
                    }
                    return spawned;
                }
                else
                {
                    LoggerHelper.VerboseLog($"No spawn stat found for monster type {monsterType}");
                }
            }
            else
            {
                LoggerHelper.VerboseLog($"No factory found for monster type {monsterType}");
            }
            return false;
        }

        private void AddWanderingMonsters()
        {
            foreach (
                var (monster, monsterMax) in _maxMonsterInTomb.Where(m => IsWanderer(m.Key)).Take(2)
            )
            {
                var total = _mineShaft.mineRandom.Next(monsterMax + 1);

                var areasForMonster = TombAreas
                    .Where(a => a.AllowedMonsters.Contains(monster))
                    .ToArray();

                if (areasForMonster.Length == 0)
                {
                    // Log that there are no areas for this monster
                    LoggerHelper.Log($"No areas available for monster: {monster}", LogLevel.Warn);
                    continue; // Skip to the next monster
                }

                var monstersPerArea = total / areasForMonster.Length;
                var remainingMonsters = total % areasForMonster.Length;

                foreach (var area in areasForMonster)
                {
                    var monsterStats = area.MonstersSpawnStats[monster];
                    var monstersToSpawn = monstersPerArea + (remainingMonsters-- > 0 ? 1 : 0);

                    for (int i = 0; i < monstersToSpawn; i++)
                    {
                        Func<Vector2, Monster> factory = (Vector2 position) =>
                            MonsterFactory[monster]
                                (
                                    position,
                                    monsterStats.Difficulty,
                                    monsterStats.Speed,
                                    TexturesNames[monster]
                                );
                        bool spawned = AddMonsters(factory, totalOfTries: 100);
                    }
                }
            }
        }

        private bool AddMonsters<T>(Func<Vector2, T> factory, Vector2 position)
            where T : Monster
        {
            var monster = factory(Vector2.Zero); // Create a new T using the factory delegate

            return TryToAddMonster(monster, position, _mineShaft);
        }

        private bool AddMonsters<T>(Func<Vector2, T> factory, TombArea area)
            where T : Monster
        {
            var monster = factory(Vector2.Zero); // Create a new T using the factory delegate

            Vector2? chosenTile;
            chosenTile = area.ValidTiles.GetRandomItem(t => 1);

            if (chosenTile.HasValue)
            {
                area.ValidTiles.Remove(chosenTile.Value);
                return TryToAddMonster(monster, (Vector2)chosenTile, _mineShaft);
            }
            return false;
        }

        private bool AddMonsters<T>(
            Func<Vector2, T> factory,
            HashSet<Rectangle>? excludedAreas = null,
            int totalOfTries = 100
        )
            where T : Monster
        {
            var monster = factory(Vector2.Zero); // Create a new T using the factory delegate
            int iteration = 0;
            while (iteration < totalOfTries) // add a maximum number of iterations
            {
                Vector2? chosenTile = null;

                for (int i = 0; i < totalOfTries; i++)
                {
                    chosenTile = TileUtility.GetRandomTile(
                        _mineShaft,
                        TileUtility.ValidMineTileIndexes,
                        excludedAreas
                    );

                    if (chosenTile != null)
                    {
                        break;
                    }
                }

                if (chosenTile.HasValue)
                {
                    return TryToAddMonster(monster, (Vector2)chosenTile, _mineShaft);
                }
                iteration++;
            }
            return false;
        }

        private bool TryToAddMonster(
            Monster m,
            Vector2 tile,
            MineShaft _mineShaft,
            bool moving = false,
            (int minThreshold, int maxThreshold)? threshold = null
        )
        {
            var conditions =
                TileUtility.IsTileClearForMonsters(tile, _mineShaft)
                && !_mineShaft.IsTileOccupiedBy(tile);
            if (moving && threshold.HasValue)
            {
                conditions &= WithinPlayerThreshold(threshold.Value, tile);
            }
            if (conditions)
            {
                if (m is Duggy)
                {
                    var validIndexes = new int[] { 165, 181, 183 };
                    var backTile = _mineShaft.getTileIndexAt((int)tile.X, (int)tile.Y, "Back");
                    if (!validIndexes.Contains(backTile))
                    {
                        return false;
                    }
                }
                if (moving)
                {
                    m.setTileLocation(tile);
                }
                else
                {
                    m.setTilePosition((int)tile.X, (int)tile.Y);
                }

                if (!moving)
                {
                    _mineShaft.characters.Add(m);
                }
                return true;
            }

            return false;
        }

        private bool WithinPlayerThreshold((int min, int max) chosenThreshold, Vector2 monsterTile)
        {
            var playerTile = Game1.MasterPlayer.Tile;

            // Calculate the threshold (Euclidean distance between player and monster tiles)
            float threshold = (float)
                Math.Sqrt(
                    Math.Pow(monsterTile.X - playerTile.X, 2)
                        + Math.Pow(monsterTile.Y - playerTile.Y, 2)
                );

            // Check if the threshold is within the given min and max parameters
            bool withinThreshold =
                threshold >= chosenThreshold.min && threshold <= chosenThreshold.max;

            return withinThreshold;
        }

        private bool IfIsWithinMaxLimit(TombMonsterType monsterType, TombArea area)
        {
            if (area.MonsterCounter[monsterType] >= area.MonstersSpawnStats[monsterType].Max)
            {
                return false;
            }
            if (TombMonsterCounter[monsterType] >= _maxMonsterInTomb[monsterType])
            {
                return false;
            }
            return true;
        }
        #endregion
    }
}
