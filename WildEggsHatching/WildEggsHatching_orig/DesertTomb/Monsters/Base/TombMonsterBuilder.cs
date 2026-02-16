using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Monsters;
using WildEggsHatching.DesertTomb.Spawning;
using WildEggsHatching.DesertTomb.Configuration;
using WildEggsHatching.DesertTomb.Utilities;

namespace WildEggsHatching.DesertTomb.Monsters.Base
{
    internal static class TombMonsterBuilder
    {
        public static void SetupBasicMonster(
            Monster monster,
            int difficulty,
            string textureName,
            int speedToAdd,
            string? customDropData = null
        )
        {
            monster.Speed += speedToAdd;
            monster.MaxHealth = (int)(
                monster.MaxHealth
                * SpawnConfiguration.Default.Multipliers.EnhancedMonsterHealthMultiplier
            );
            monster.Health = monster.MaxHealth;

            monster.resilience.Value = monster.resilience.Value <= 0 ? 1 : monster.resilience.Value;
            monster.resilience.Value += SpawnConfiguration
                .Default
                .Multipliers
                .EnhancedMonsterResilienceIncrement;

            monster.BuffForAdditionalDifficulty(difficulty);
            monster.Sprite.LoadTexture($"Characters\\Monsters\\{textureName.Replace(" ", "")}");

            monster.objectsToDrop.Clear();
            monster.objectsToDrop.AddRange(
                string.IsNullOrEmpty(customDropData)
                    ? MonsterItemHelper.GetModifiedMonsterItems(monster)
                    : MonsterItemHelper.GetModifiedMonsterItems(monster, customDropData)
            );
        }

        public static void SetupMonsterWithDoubleDrops(
            Monster monster,
            MonsterMultipliers monsterMultipliers,
            int difficulty,
            string textureName,
            int speedToAdd,
            string customDropData = null
        )
        {
            SetupBasicMonster(monster, difficulty, textureName, speedToAdd, customDropData);

            // Apply double drop items based on difficulty
            if (Game1.random.NextDouble() < monsterMultipliers.ChanceToDoubleDropItem)
            {
                var additionalItems = string.IsNullOrEmpty(customDropData)
                    ? MonsterItemHelper.GetModifiedMonsterItems(monster)
                    : MonsterItemHelper.GetModifiedMonsterItems(monster, customDropData);
                monster.objectsToDrop.AddRange(additionalItems);
            }
        }

        public static void SetMonsterNames(Monster monster, string baseName)
        {
            monster.Name = $"Tomb {baseName}";
            monster.displayName = $"Tomb {baseName}";
        }
    }
}
