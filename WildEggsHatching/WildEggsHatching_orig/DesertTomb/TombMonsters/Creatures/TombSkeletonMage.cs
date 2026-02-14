using Microsoft.Xna.Framework;
using StardewValley.Monsters;
using WildEggsHatching.DesertTomb.Configuration;
using WildEggsHatching.DesertTomb.Generation;
using WildEggsHatching.DesertTomb.Utilities;
using static WildEggsHatching.EnumsAndStrings.Enums;

namespace WildEggsHatching.DesertTomb.TombMonsters.Creatures
{
    internal class TombSkeletonMage : Skeleton
    {
        internal TombSkeletonMage(
            MonsterMultipliers monsterMultipliers,
            Vector2 position,
            int difficulty = 3,
            string textureName = "SkeletonMage",
            int speedToAdd = 0
        )
            : base(position, true)
        {
            Speed += speedToAdd;
            BuffForAdditionalDifficulty(difficulty);
            MaxHealth = (int)(
                MaxHealth * SpawnConfiguration.Default.Multipliers.EnhancedMonsterHealthMultiplier
            );
            Health = MaxHealth;
            resilience.Value = resilience.Value <= 0 ? 1 : resilience.Value;
            resilience.Value += SpawnConfiguration
                .Default
                .Multipliers
                .EnhancedMonsterResilienceIncrement;
            Sprite.LoadTexture($"Characters\\Monsters\\{textureName}");
            objectsToDrop.Clear();
            objectsToDrop.AddRange(
                MonsterItemHelper.GetModifiedMonsterItems(TombMonsterType.Skeleton)
            );
            Name = "Tomb Skeleton Mage";
            displayName = "Tomb Skeleton Mage";
        }
    }
}
