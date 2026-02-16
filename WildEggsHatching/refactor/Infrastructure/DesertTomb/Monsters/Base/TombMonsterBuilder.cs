using StardewValley;
using StardewValley.Monsters;
using WildEggsHatching.DesertTomb.Core.Configuration;
using WildEggsHatching.Infrastructure.DesertTomb.Loot;

namespace WildEggsHatching.Infrastructure.DesertTomb.Monsters.Base;

/// <summary>
/// Common monster setup logic (health/resilience/drops) shared across DesertTomb monsters.
/// </summary>
internal static class TombMonsterBuilder
{
    public static void SetupBasicMonster(
        Monster monster,
        MonsterMultipliers multipliers,
        int difficulty,
        string textureName,
        int speedToAdd,
        bool luckyBonusEnabled,
        string? customDropData = null
    )
    {
        monster.Speed += speedToAdd;

        monster.MaxHealth = (int)(monster.MaxHealth * multipliers.EnhancedMonsterHealthMultiplier);
        monster.Health = monster.MaxHealth;

        monster.resilience.Value = monster.resilience.Value <= 0 ? 1 : monster.resilience.Value;
        monster.resilience.Value += multipliers.EnhancedMonsterResilienceIncrement;

        monster.BuffForAdditionalDifficulty(difficulty);
        monster.Sprite.LoadTexture($"Characters\\Monsters\\{textureName.Replace(" ", "")}");

        monster.objectsToDrop.Clear();
        monster.objectsToDrop.AddRange(
            string.IsNullOrEmpty(customDropData)
                ? MonsterItemHelper.GetModifiedMonsterItems(monster, luckyBonusEnabled)
                : MonsterItemHelper.GetModifiedMonsterItems(monster, luckyBonusEnabled, customDropData)
        );
    }

    public static void SetupMonsterWithDoubleDrops(
        Monster monster,
        MonsterMultipliers multipliers,
        int difficulty,
        string textureName,
        int speedToAdd,
        bool luckyBonusEnabled,
        string? customDropData = null
    )
    {
        SetupBasicMonster(monster, multipliers, difficulty, textureName, speedToAdd, luckyBonusEnabled, customDropData);

        if (Game1.random.NextDouble() < multipliers.ChanceToDoubleDropItem)
        {
            var additionalItems = string.IsNullOrEmpty(customDropData)
                ? MonsterItemHelper.GetModifiedMonsterItems(monster, luckyBonusEnabled)
                : MonsterItemHelper.GetModifiedMonsterItems(monster, luckyBonusEnabled, customDropData);

            monster.objectsToDrop.AddRange(additionalItems);
        }
    }

    public static void SetMonsterNames(Monster monster, string baseName)
    {
        monster.Name = $"Tomb {baseName}";
        monster.displayName = $"Tomb {baseName}";
    }
}
