using StardewValley.Monsters;
using WildEggsHatching.DesertTomb.Core.Configuration;

namespace WildEggsHatching.Infrastructure.DesertTomb.Monsters.Base;

/// <summary>
/// Common monster setup logic (speed/health/resilience/stats) shared across DesertTomb monsters.
/// Drops are applied by <c>DesertTombMonsterFactory</c> using JSON drop tables.
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

        // Phase 6: drop strings removed; drops are JSON-driven and applied by the factory.
        monster.objectsToDrop.Clear();
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
        // Drops are handled centrally by the factory, so this is now equivalent to basic setup.
        SetupBasicMonster(monster, multipliers, difficulty, textureName, speedToAdd, luckyBonusEnabled, customDropData);
    }

    public static void SetMonsterNames(Monster monster, string baseName)
    {
        monster.Name = $"Tomb {baseName}";
        monster.displayName = $"Tomb {baseName}";
    }
}
