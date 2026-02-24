using System;

namespace EggsInWild.DesertTomb.Core.Configuration;

public class ThePharaohConfiguration
    {
        // ===== HEALING CONFIGURATION =====
        public float HealPercent { get; set; } = 0.04f;
        public float SelfHealCooldown { get; set; } = 1000f;
        public float CooldownMultiplier { get; set; } = 8.0f;
        public float MinHealHealth { get; set; } = 0.25f;
        public float SelfHealHealth { get; set; } = 0.25f;
        public float HealGrowthRate { get; set; } = 0.005f;
        public float InitialHealPercent { get; set; } = 0.02f;

        // ===== COMBAT CONFIGURATION =====
        public float AttackCooldown { get; set; } = 4000f;
        public float RandomCastChance { get; set; } = 0.015f;
        public float AttackInterval { get; set; } = 350f;
        public float FreezeChance { get; set; } = 0.35f;
        public float CloseRangeFreezeBonus { get; set; } = 0.1f;

        // ===== STATE TRANSITION CONFIGURATION =====
        public float MageModeHealth { get; set; } = 0.45f;
        public float MageModeExitHealth { get; set; } = 0.75f;
        public float FirstTransformHealthBoost { get; set; } = 0.3f;

        // ===== VISION AND MOVEMENT CONFIGURATION =====
        public int VisionRange { get; set; } = 8;
        public int MaxFleeAttempts { get; set; } = 200;
        public float FleeDistanceScale { get; set; } = 20f;
        public float FleeSpeedBonus { get; set; } = 2f;

        // ===== CHANCE CONFIGURATION =====
        public double AccuracyLossPerStack { get; set; } = 0.01;
        public double AggroStackRate { get; set; } = 0.015;
        public double ProximityEscapeChance { get; set; } = 0.01;
        public double DesperateEscapeChance { get; set; } = 0.2;
        public double VanishChance { get; set; } = 0.1;

        // ===== THRESHOLD CONFIGURATION =====
        public int MaxInterruptions { get; set; } = 3;
        public int MinPlayers { get; set; } = 2;
        public int MaxPlayers { get; set; } = 3;
        public int AllyRange { get; set; } = 6;
        public int MaxAccuracyStacks { get; set; } = 20;
        public int MaxAggroStacks { get; set; } = 10;

        // ===== MISS CHANCE CONFIGURATION =====
        public float BaseMissChance { get; set; } = 0.05f;
        public float HealAccuracyPenalty { get; set; } = 1.01f;
        public float FadeMissBonus { get; set; } = 2f;

        // ===== VISUAL CONFIGURATION =====
        public float FadeDuration { get; set; } = 2000f;
        public float WarriorScale { get; set; } = 1f;
        public float MageScale { get; set; } = 1.4f;

        public ThePharaohConfiguration Clone(SpawnConfiguration config)
        {
            return new ThePharaohConfiguration
            {
                // HEALING CONFIGURATION
                HealPercent = config.GetDifficultyModifiedValueMult(this.HealPercent),
                SelfHealCooldown = config.GetDifficultyModifiedValueMult(
                    this.SelfHealCooldown,
                    false
                ),
                CooldownMultiplier = config.GetDifficultyModifiedValueMult(
                    this.CooldownMultiplier,
                    false
                ),
                MinHealHealth = config.GetDifficultyModifiedValueMult(this.MinHealHealth),
                SelfHealHealth = config.GetDifficultyModifiedValueMult(this.SelfHealHealth),
                HealGrowthRate = config.GetDifficultyModifiedValueMult(this.HealGrowthRate),
                InitialHealPercent = config.GetDifficultyModifiedValueMult(this.InitialHealPercent),

                // COMBAT CONFIGURATION
                AttackCooldown = config.GetDifficultyModifiedValueMult(this.AttackCooldown, false),
                RandomCastChance = config.GetDifficultyModifiedValueMult(this.RandomCastChance),
                AttackInterval = config.GetDifficultyModifiedValueMult(this.AttackInterval, false),
                FreezeChance = config.GetDifficultyModifiedValueMult(this.FreezeChance),
                CloseRangeFreezeBonus = config.GetDifficultyModifiedValueMult(
                    this.CloseRangeFreezeBonus
                ),

                // STATE TRANSITION CONFIGURATION
                MageModeHealth = config.GetDifficultyModifiedValueMult(this.MageModeHealth),
                MageModeExitHealth = config.GetDifficultyModifiedValueMult(this.MageModeExitHealth),
                FirstTransformHealthBoost = config.GetDifficultyModifiedValueMult(
                    this.FirstTransformHealthBoost
                ),

                // VISION AND MOVEMENT CONFIGURATION
                VisionRange = config.GetDifficultyModifiedValueInteger(this.VisionRange),
                MaxFleeAttempts = config.GetDifficultyModifiedValueInteger(this.MaxFleeAttempts),
                FleeDistanceScale = config.GetDifficultyModifiedValueMult(this.FleeDistanceScale),
                FleeSpeedBonus = config.GetDifficultyModifiedValueMult(this.FleeSpeedBonus),

                // CHANCE CONFIGURATION
                AccuracyLossPerStack = config.GetDifficultyModifiedValueMult(
                    (float)this.AccuracyLossPerStack
                ),
                AggroStackRate = config.GetDifficultyModifiedValueMult((float)this.AggroStackRate),
                ProximityEscapeChance = config.GetDifficultyModifiedValueMult(
                    (float)this.ProximityEscapeChance
                ),
                DesperateEscapeChance = config.GetDifficultyModifiedValueMult(
                    (float)this.DesperateEscapeChance
                ),
                VanishChance = config.GetDifficultyModifiedValueMult((float)this.VanishChance),

                // THRESHOLD CONFIGURATION
                MaxInterruptions = config.GetDifficultyModifiedValueInteger(this.MaxInterruptions),
                MinPlayers = config.GetDifficultyModifiedValueInteger(this.MinPlayers),
                MaxPlayers = config.GetDifficultyModifiedValueInteger(this.MaxPlayers),
                AllyRange = config.GetDifficultyModifiedValueInteger(this.AllyRange),
                MaxAccuracyStacks = config.GetDifficultyModifiedValueInteger(
                    this.MaxAccuracyStacks
                ),
                MaxAggroStacks = config.GetDifficultyModifiedValueInteger(this.MaxAggroStacks),

                // MISS CHANCE CONFIGURATION
                BaseMissChance = config.GetDifficultyModifiedValueMult(this.BaseMissChance),
                HealAccuracyPenalty = config.GetDifficultyModifiedValueMult(
                    this.HealAccuracyPenalty,
                    false
                ),
                FadeMissBonus = config.GetDifficultyModifiedValueMult(this.FadeMissBonus),

                // VISUAL CONFIGURATION
                FadeDuration = this.FadeDuration,
                WarriorScale = this.WarriorScale,
                MageScale = this.MageScale,
            };
        }
    }
