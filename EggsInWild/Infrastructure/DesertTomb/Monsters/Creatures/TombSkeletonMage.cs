using Microsoft.Xna.Framework;
using StardewValley.Monsters;
using EggsInWild.DesertTomb.Core.Configuration;
using EggsInWild.Infrastructure.DesertTomb.Monsters.Base;

namespace EggsInWild.Infrastructure.DesertTomb.Monsters.Creatures;

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

        Name = "Tomb Skeleton Mage";
        displayName = "Tomb Skeleton Mage";
    }
}
