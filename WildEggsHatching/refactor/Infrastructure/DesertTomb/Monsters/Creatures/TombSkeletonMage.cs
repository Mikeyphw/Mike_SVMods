using Microsoft.Xna.Framework;
using StardewValley.Monsters;
using WildEggsHatching.DesertTomb.Core.Configuration;
using WildEggsHatching.Infrastructure.DesertTomb.Loot;
using WildEggsHatching.Infrastructure.DesertTomb.Monsters.Base;
using static WildEggsHatching.DesertTomb.Core.Enums.DesertTombEnums;

namespace WildEggsHatching.Infrastructure.DesertTomb.Monsters.Creatures;

internal sealed class TombSkeletonMage : Skeleton
{
    internal TombSkeletonMage(
        MonsterMultipliers multipliers,
        Vector2 position,
        int difficulty = 3,
        string textureName = "Skeleton Mage",
        int speedToAdd = 0,
        string? customDropData = null
    )
        : base(position, true)
    {
        TombMonsterBuilder.SetupBasicMonster(
            this,
            multipliers,
            difficulty,
            textureName,
            speedToAdd,
            luckyBonusEnabled: true,
            customDropData: customDropData
        );

        // Legacy behavior: mage uses the skeleton drop table.
        objectsToDrop.Clear();
        objectsToDrop.AddRange(MonsterItemHelper.GetModifiedMonsterItems(TombMonsterType.Skeleton, luckyBonusEnabled: true));

        Name = "Tomb Skeleton Mage";
        displayName = "Tomb Skeleton Mage";
    }
}
