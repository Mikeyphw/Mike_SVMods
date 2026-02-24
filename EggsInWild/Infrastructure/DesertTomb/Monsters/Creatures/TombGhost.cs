using Microsoft.Xna.Framework;
using StardewValley.Monsters;
using EggsInWild.DesertTomb.Core.Configuration;
using EggsInWild.Infrastructure.DesertTomb.Monsters.Base;

namespace EggsInWild.Infrastructure.DesertTomb.Monsters.Creatures;

internal sealed class TombGhost : Ghost
{
    internal TombGhost(
        MonsterMultipliers multipliers,
        Vector2 position,
        int difficulty = 2,
        string textureName = "Carbon Ghost",
        int speedToAdd = 0,
        string? customDropData = null
    )
        : base(position)
    {
        TombMonsterBuilder.SetupBasicMonster(
            this,
            multipliers,
            difficulty,
            textureName,
            speedToAdd,
            luckyBonusEnabled: true,
            customDropData: customDropData ?? "164 0.016"
        );

        TombMonsterBuilder.SetMonsterNames(this, "Ghost");
    }
}
