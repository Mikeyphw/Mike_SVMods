using Microsoft.Xna.Framework;
using StardewValley.Monsters;
using EggsInWild.DesertTomb.Core.Configuration;
using EggsInWild.Infrastructure.DesertTomb.Monsters.Base;

namespace EggsInWild.Infrastructure.DesertTomb.Monsters.Creatures;

internal sealed class TombMummy : Mummy
{
    internal TombMummy(
        MonsterMultipliers multipliers,
        Vector2 position,
        int difficulty = 3,
        string textureName = "Mummy",
        int speedToAdd = 0,
        string? customDropData = null
    )
        : base(position)
    {
        TombMonsterBuilder.SetupMonsterWithDoubleDrops(
            this,
            multipliers,
            difficulty,
            textureName,
            speedToAdd,
            luckyBonusEnabled: true,
            customDropData: customDropData
        );

        TombMonsterBuilder.SetMonsterNames(this, "Mummy");
    }
}
