using System.Collections.Generic;
using WildEggsHatching.DesertTomb.Configuration;
using WildEggsHatching.DesertTomb.TombMonsters.Base;
using static WildEggsHatching.EnumsAndStrings.Enums;
using static WildEggsHatching.EnumsAndStrings.Enums.TextOptionValues;

namespace WildEggsHatching.DesertTomb.Configuration
{
    /// <summary>
    /// Centralized configuration for all spawn rates, multipliers, and balance values
    /// </summary>
    public class SpawnConfiguration
    {
        public TombDifficulty Difficulty { get; set; } = TombDifficulty.Moderate;
        public GlobalConfiguration Global { get; set; } = new();
        public MonsterMultipliers Multipliers { get; set; } = new();
        public ThePharaohConfiguration ThePharaoh { get; set; } = new();

        /// <summary>
        /// Configuration for allowed monsters by difficulty level
        /// </summary>
        public Dictionary<
            TombDifficulty,
            HashSet<TombMonsterType>
        > AllowedMonstersByDifficulty { get; set; } = new();

        /// <summary>
        /// Configuration for monster spawn rates by area type and difficulty
        /// </summary>
        internal Dictionary<
            TombAreaType,
            Dictionary<TombMonsterType, MonsterSpawnStats>
        > MonsterSpawnRatesByArea { get; set; } = new();

        /// <summary>
        /// Configuration for difficulty-based adjustments
        /// </summary>
        public DifficultyAdjustmentConfiguration DifficultyAdjustments { get; set; } = new();

        /// <summary>
        /// Random chance modifiers for different egg types (moved from EggSpawning.cs)
        /// </summary>
        public RandomChanceModifiers RandomChanceModifiers { get; set; } = new();

        public static SpawnConfiguration Default =>
            new()
            {
                Global = new GlobalConfiguration
                {
                    MaxMonstersPerArea = 50,
                    MinMonstersPerArea = 1,
                    SpawnAttemptsPerTile = 100,
                    LuckyBonusEnabled = true,
                    ScrollProgressionReduction = 0.5,
                },
                Multipliers = new MonsterMultipliers
                {
                    EnhancedMonsterHealthMultiplier = 1.5f,
                    QuestCompletionDifficultyReduction = 0.5f,
                    ModifierCalculationMultiplier = 1.5f,
                    GlobalDamageMultiplier = 0.75f,
                    GlobalHealthMultiplier = 0.8f,
                    HealingReduction = 0.5f,
                    AttackIntervalMultiplier = 1.4f,
                    DropRateMultiplier = 1.15f,
                    EnhancedMonsterResilienceIncrement = 2,
                },
                AllowedMonstersByDifficulty = new()
                {
                    [TombDifficulty.Easy] = new HashSet<TombMonsterType>
                    {
                        TombMonsterType.Bat,
                        TombMonsterType.Mummy,
                        TombMonsterType.Ghost,
                        TombMonsterType.Duggy,
                        TombMonsterType.TheTombPharaoh,
                    },
                    [TombDifficulty.Moderate] = new HashSet<TombMonsterType>
                    {
                        TombMonsterType.Bat,
                        TombMonsterType.Mummy,
                        TombMonsterType.Ghost,
                        TombMonsterType.Duggy,
                        TombMonsterType.TheTombPharaoh,
                        TombMonsterType.AssassinBug,
                        TombMonsterType.Spider,
                        TombMonsterType.BruteMummy,
                        TombMonsterType.Skeleton,
                        TombMonsterType.SkeletonMage,
                    },
                    [TombDifficulty.Hard] = new HashSet<TombMonsterType>
                    {
                        TombMonsterType.Bat,
                        TombMonsterType.Mummy,
                        TombMonsterType.Ghost,
                        TombMonsterType.Duggy,
                        TombMonsterType.TheTombPharaoh,
                        TombMonsterType.AssassinBug,
                        TombMonsterType.Spider,
                        TombMonsterType.BruteMummy,
                        TombMonsterType.Skeleton,
                        TombMonsterType.SkeletonMage,
                        TombMonsterType.RoyalSerpent,
                        TombMonsterType.HauntedSkull,
                    },
                },
                MonsterSpawnRatesByArea = new()
                {
                    [TombAreaType.MummyArea] = new Dictionary<TombMonsterType, MonsterSpawnStats>
                    {
                        [TombMonsterType.TheTombPharaoh] = new(1, 2, 0),
                    },
                    [TombAreaType.MummyDefendArea] = new Dictionary<
                        TombMonsterType,
                        MonsterSpawnStats
                    >
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
                },
            };

        public SpawnConfiguration Clone()
        {
            var clone = new SpawnConfiguration
            {
                Global = this.Global.Clone(this),
                Multipliers = this.Multipliers.Clone(this),
                ThePharaoh = this.ThePharaoh.Clone(this),
                DifficultyAdjustments = this.DifficultyAdjustments.Clone(),
                RandomChanceModifiers = this.RandomChanceModifiers.Clone(this),
            };

            // Deep clone AreaConfigurations
            // Removed AreaConfigurations cloning

            // Deep clone AllowedMonstersByDifficulty
            foreach (var kvp in this.AllowedMonstersByDifficulty)
            {
                clone.AllowedMonstersByDifficulty[kvp.Key] = new HashSet<TombMonsterType>(
                    kvp.Value
                );
            }

            // Deep clone MonsterSpawnRatesByArea
            foreach (var areaKvp in this.MonsterSpawnRatesByArea)
            {
                var innerClone = new Dictionary<TombMonsterType, MonsterSpawnStats>();
                foreach (var monsterKvp in areaKvp.Value)
                {
                    innerClone[monsterKvp.Key] = monsterKvp.Value.Clone();
                }
                clone.MonsterSpawnRatesByArea[areaKvp.Key] = innerClone;
            }

            return clone;
        }

        public void DeepCopySpawnConfiguration(
            SpawnConfiguration original,
            TombDifficulty tombDifficulty
        )
        {
            Difficulty = tombDifficulty;
            Global = original.Global.Clone(this);
            Multipliers = original.Multipliers.Clone(this);
            ThePharaoh = original.ThePharaoh.Clone(this);
            DifficultyAdjustments = original.DifficultyAdjustments.Clone();
            RandomChanceModifiers = original.RandomChanceModifiers.Clone(this);

            // Deep copy AreaConfigurations
            // Removed AreaConfigurations deep copy

            // Deep copy AllowedMonstersByDifficulty
            foreach (var kvp in original.AllowedMonstersByDifficulty)
            {
                AllowedMonstersByDifficulty[kvp.Key] = new HashSet<TombMonsterType>(kvp.Value);
            }

            // Deep copy MonsterSpawnRatesByArea
            foreach (var areaKvp in original.MonsterSpawnRatesByArea)
            {
                var innerCopy = new Dictionary<TombMonsterType, MonsterSpawnStats>();
                foreach (var monsterKvp in areaKvp.Value)
                {
                    innerCopy[monsterKvp.Key] = monsterKvp.Value.Clone();
                }
                MonsterSpawnRatesByArea[areaKvp.Key] = innerCopy;
            }
        }

        public float GetDifficultyModifiedValueMult(float original, bool increase = true)
        {
            if (Difficulty == TombDifficulty.Moderate)
            {
                return original;
            }
            increase ^= Difficulty == TombDifficulty.Easy;
            var modifiers = DifficultyAdjustments.DifficultyModifierMult[Difficulty];
            var difficultyMultiplier = increase ? 1 + modifiers.ToIncrease : modifiers.ToDecrease;
            var result = original * difficultyMultiplier;
            return Math.Max(0, result);
        }

        public int GetDifficultyModifiedValueInteger(int original, bool increase = true)
        {
            if (Difficulty == TombDifficulty.Moderate)
            {
                return original;
            }
            increase ^= Difficulty == TombDifficulty.Easy;
            var modifiers = DifficultyAdjustments.DifficultyModifierMult[Difficulty];
            var difficultyIncrement = increase ? modifiers.ToIncrease : 0 - modifiers.ToDecrease;
            var result = original + (int)difficultyIncrement;
            return Math.Max(0, result);
        }
    }

    public class AreaConfiguration
    {
        public Dictionary<TombMonsterType, SpawnRate> BaseSpawnRates { get; set; } = new();
        public float AreaMultiplier { get; set; } = 1.0f;

        public AreaConfiguration Clone()
        {
            var clonedConfig = new AreaConfiguration
            {
                AreaMultiplier = this.AreaMultiplier,
                BaseSpawnRates = new Dictionary<TombMonsterType, SpawnRate>(),
            };

            foreach (var kvp in this.BaseSpawnRates)
            {
                clonedConfig.BaseSpawnRates[kvp.Key] = kvp.Value.Clone();
            }

            return clonedConfig;
        }
    }

    public class SpawnRate
    {
        public int Min { get; set; }
        public int Max { get; set; }
        public double Weight { get; set; } = 1.0;

        public SpawnRate Clone()
        {
            return new()
            {
                Min = this.Min,
                Max = this.Max,
                Weight = this.Weight,
            };
        }
    }

    public class GlobalConfiguration
    {
        public int MaxMonstersPerArea { get; set; } = 50;
        public Dictionary<TombMonsterType, int> MaxMonsterInTomb { get; }
        public int MinMonstersPerArea { get; set; } = 1;

        /// <summary>
        /// Multiplier for mine-related calculations
        /// </summary>
        public double MineMultiplier { get; set; } = 1.0;
        public int SpawnAttemptsPerTile { get; set; } = 100;
        public bool LuckyBonusEnabled { get; set; } = true;
        public double ScrollProgressionReduction { get; set; } = 0.5;

        /// <summary>
        /// Chance for barrels to appear instead of plants (moved from RefactoredDifficultyManager)
        /// </summary>
        public double BarrelPlantChance { get; set; } = 0.5;

        public GlobalConfiguration()
        {
            MaxMonsterInTomb = new Dictionary<TombMonsterType, int>()
            {
                { TombMonsterType.AssassinBug, 5 },
                { TombMonsterType.Bat, 2 },
                { TombMonsterType.BruteMummy, 6 },
                { TombMonsterType.Duggy, 5 },
                { TombMonsterType.RoyalSerpent, 1 },
                { TombMonsterType.HauntedSkull, 1 },
                { TombMonsterType.Ghost, 2 },
                { TombMonsterType.Mummy, 50 },
                { TombMonsterType.Skeleton, 6 },
                { TombMonsterType.SkeletonMage, 3 },
                { TombMonsterType.Spider, 2 },
                { TombMonsterType.TheTombPharaoh, 1 },
            };
        }

        public GlobalConfiguration Clone(SpawnConfiguration config)
        {
            var clone = new GlobalConfiguration
            {
                MaxMonstersPerArea = config.GetDifficultyModifiedValueInteger(
                    this.MaxMonstersPerArea
                ),
                MinMonstersPerArea = config.GetDifficultyModifiedValueInteger(
                    this.MinMonstersPerArea
                ),
                MineMultiplier = config.GetDifficultyModifiedValueMult((float)this.MineMultiplier),
                SpawnAttemptsPerTile = config.GetDifficultyModifiedValueInteger(
                    this.SpawnAttemptsPerTile
                ),
                LuckyBonusEnabled = this.LuckyBonusEnabled,
                ScrollProgressionReduction = config.GetDifficultyModifiedValueMult(
                    (float)this.ScrollProgressionReduction
                ),
                BarrelPlantChance = config.GetDifficultyModifiedValueMult(
                    (float)this.BarrelPlantChance
                ),
            };

            // Deep copy the dictionary
            foreach (var kvp in this.MaxMonsterInTomb)
            {
                clone.MaxMonsterInTomb.Add(kvp.Key, kvp.Value);
            }

            return clone;
        }
    }

    public class MonsterMultipliers
    {
        /// <summary>
        /// Health multiplier for enhanced monsters (was 1.5x hardcoded in monster classes)
        /// </summary>
        public float EnhancedMonsterHealthMultiplier { get; set; } = 1.5f;

        /// <summary>
        /// Difficulty reduction multiplier for quest completion (was 0.5x in RefactoredDifficultyManager)
        /// </summary>
        public float QuestCompletionDifficultyReduction { get; set; } = 0.5f;

        /// <summary>
        /// Modifier calculation multiplier (was 1.5x in RefactoredDifficultyManager)
        /// </summary>
        public float ModifierCalculationMultiplier { get; set; } = 1.5f;

        /// <summary>
        /// Global damage multiplier for all monsters (0.5 = 50% damage)
        /// </summary>
        public float GlobalDamageMultiplier { get; set; } = 0.75f;

        /// <summary>
        /// Global health multiplier for all monsters (0.8 = 80% health)
        /// </summary>
        public float GlobalHealthMultiplier { get; set; } = 0.8f;

        /// <summary>
        /// Healing effectiveness reduction (0.5 = 50% less healing)
        /// </summary>
        public float HealingReduction { get; set; } = 0.5f;

        /// <summary>
        /// Attack frequency reduction (1.0 = normal, 1.5 = 50% slower)
        /// </summary>
        public float AttackIntervalMultiplier { get; set; } = 1.4f;

        /// <summary>
        /// Drop rate adjustment to maintain reward balance
        /// </summary>
        public float DropRateMultiplier { get; set; } = 1.15f;

        public int EnhancedMonsterResilienceIncrement { get; set; } = 2;

        /// <summary>
        /// Chance to double drop items from monsters
        /// </summary>
        public double ChanceToDoubleDropItem { get; set; } = 0.4;

        /// <summary>
        /// Multiplier for item drop chance
        /// </summary>
        public double DropItemChanceMultiplier { get; set; } = 1.05;

        /// <summary>
        /// Multiplier for item drop chance specifically for The Tomb Pharaoh
        /// </summary>
        public double DropItemChanceMultiplierTheTombPharaoh { get; set; } = 1.1;

        public MonsterMultipliers Clone(SpawnConfiguration spawnConfiguration)
        {
            return new MonsterMultipliers
            {
                EnhancedMonsterHealthMultiplier = spawnConfiguration.GetDifficultyModifiedValueMult(
                    this.EnhancedMonsterHealthMultiplier
                ),
                QuestCompletionDifficultyReduction =
                    spawnConfiguration.GetDifficultyModifiedValueMult(
                        this.QuestCompletionDifficultyReduction
                    ),
                ModifierCalculationMultiplier = spawnConfiguration.GetDifficultyModifiedValueMult(
                    this.ModifierCalculationMultiplier
                ),
                GlobalDamageMultiplier = spawnConfiguration.GetDifficultyModifiedValueMult(
                    this.GlobalDamageMultiplier
                ),
                GlobalHealthMultiplier = spawnConfiguration.GetDifficultyModifiedValueMult(
                    this.GlobalHealthMultiplier
                ),
                HealingReduction = spawnConfiguration.GetDifficultyModifiedValueMult(
                    this.HealingReduction,
                    false
                ),
                AttackIntervalMultiplier = spawnConfiguration.GetDifficultyModifiedValueMult(
                    this.AttackIntervalMultiplier,
                    false
                ),
                DropRateMultiplier = spawnConfiguration.GetDifficultyModifiedValueMult(
                    this.DropRateMultiplier
                ),
                EnhancedMonsterResilienceIncrement =
                    spawnConfiguration.GetDifficultyModifiedValueInteger(
                        this.EnhancedMonsterResilienceIncrement
                    ),
                ChanceToDoubleDropItem = spawnConfiguration.GetDifficultyModifiedValueMult(
                    (float)this.ChanceToDoubleDropItem
                ),
                DropItemChanceMultiplier = spawnConfiguration.GetDifficultyModifiedValueMult(
                    (float)this.DropItemChanceMultiplier
                ),
                DropItemChanceMultiplierTheTombPharaoh =
                    spawnConfiguration.GetDifficultyModifiedValueMult(
                        (float)this.DropItemChanceMultiplierTheTombPharaoh
                    ),
            };
        }
    }

    public class RandomChanceModifiers
    {
        /// <summary>
        /// Modifier for large + magic eggs
        /// </summary>
        public float LargeMagicEggModifier { get; set; } = -0.13f;

        /// <summary>
        /// Modifier for large eggs only
        /// </summary>
        public float LargeEggModifier { get; set; } = -0.05f;

        /// <summary>
        /// Modifier for magic eggs only
        /// </summary>
        public float MagicEggModifier { get; set; } = -0.1f;

        /// <summary>
        /// Default modifier for regular eggs
        /// </summary>
        public float DefaultModifier { get; set; } = 0f;

        public RandomChanceModifiers Clone(SpawnConfiguration config)
        {
            return new RandomChanceModifiers
            {
                LargeMagicEggModifier = config.GetDifficultyModifiedValueMult(
                    this.LargeMagicEggModifier
                ),
                LargeEggModifier = config.GetDifficultyModifiedValueMult(this.LargeEggModifier),
                MagicEggModifier = config.GetDifficultyModifiedValueMult(this.MagicEggModifier),
                DefaultModifier = config.GetDifficultyModifiedValueMult(this.DefaultModifier),
            };
        }
    }

    public class DailyLuckModifiers
    {
        /// <summary>
        /// Modifier for super unlucky state
        /// </summary>
        public float SuperUnluckyModifier { get; set; } = -2f;

        /// <summary>
        /// Modifier for unlucky state
        /// </summary>
        public float UnluckyModifier { get; set; } = -1f;

        /// <summary>
        /// Modifier for neutral state
        /// </summary>
        public float NeutralModifier { get; set; } = 0f;

        /// <summary>
        /// Modifier for lucky state
        /// </summary>
        public float LuckyModifier { get; set; } = 1f;

        /// <summary>
        /// Modifier for super lucky state
        /// </summary>
        public float SuperLuckyModifier { get; set; } = 1.5f;

        /// <summary>
        /// Base daily luck modifier value
        /// </summary>
        public float BaseDailyLuckModifier { get; set; } = 0.02f;

        public DailyLuckModifiers Clone()
        {
            return new DailyLuckModifiers
            {
                SuperUnluckyModifier = this.SuperUnluckyModifier,
                UnluckyModifier = this.UnluckyModifier,
                NeutralModifier = this.NeutralModifier,
                LuckyModifier = this.LuckyModifier,
                SuperLuckyModifier = this.SuperLuckyModifier,
                BaseDailyLuckModifier = this.BaseDailyLuckModifier,
            };
        }
    }

    /// <summary>
    /// Configuration for monster spawn statistics
    /// </summary>
    public class MonsterSpawnStatsConfiguration
    {
        public int Max { get; set; }
        public int Min { get; set; }
        public int Difficulty { get; set; }
        public int Speed { get; set; }

        public MonsterSpawnStatsConfiguration() { }

        public MonsterSpawnStatsConfiguration(int max, int min, int difficulty, int speed)
        {
            Max = max;
            Min = min;
            Difficulty = difficulty;
            Speed = speed;
        }

        public MonsterSpawnStatsConfiguration Clone()
        {
            return new MonsterSpawnStatsConfiguration(Max, Min, Difficulty, Speed);
        }
    }

    /// <summary>
    /// Configuration for difficulty-based adjustments
    /// </summary>
    public class DifficultyAdjustmentConfiguration
    {
        public Dictionary<
            TombDifficulty,
            DifficultyModifiers
        > DifficultyModifierForInteger { get; set; } = new();
        public Dictionary<
            TombDifficulty,
            DifficultyModifiers
        > DifficultyModifierMult { get; set; } = new();

        /// <summary>
        /// Daily luck modifiers for different luck states (moved from EggSpawning.cs)
        /// </summary>
        public DailyLuckModifiers DailyLuckModifiers { get; set; } = new();

        public DifficultyAdjustmentConfiguration()
        {
            DifficultyModifierForInteger = new Dictionary<TombDifficulty, DifficultyModifiers>
            {
                [TombDifficulty.Easy] = new(1, 1),
                [TombDifficulty.Moderate] = new(2, 1),
                [TombDifficulty.Hard] = new(3, 1),
            };
            DifficultyModifierMult = new Dictionary<TombDifficulty, DifficultyModifiers>
            {
                [TombDifficulty.Easy] = new(0.2f, 0.25f),
                [TombDifficulty.Moderate] = new(0.3f, 0.25f),
                [TombDifficulty.Hard] = new(0.4f, 0.25f),
            };
        }

        public DifficultyAdjustmentConfiguration Clone()
        {
            var clone = new DifficultyAdjustmentConfiguration
            {
                DifficultyModifierForInteger = new(this.DifficultyModifierForInteger),
                DifficultyModifierMult = new(this.DifficultyModifierMult),
                DailyLuckModifiers = this.DailyLuckModifiers.Clone(),
            };
            return clone;
        }
    }

    public class DifficultyModifiers
    {
        public float ToIncrease;
        public float ToDecrease;

        public DifficultyModifiers(float toIncrease, float toDecrease)
        {
            ToIncrease = toIncrease;
            ToDecrease = toDecrease;
        }

        public DifficultyModifiers(int toIncrease, int toDecrease)
        {
            ToIncrease = toIncrease;
            ToDecrease = toDecrease;
        }

        public DifficultyModifiers Clone()
        {
            return new(this.ToDecrease, this.ToIncrease);
        }
    }
}
