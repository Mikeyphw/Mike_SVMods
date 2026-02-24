using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Monsters;
using EggsInWild.DesertTomb.Core.Configuration;
using EggsInWild.Infrastructure.DesertTomb.Monsters.Base;

namespace EggsInWild.Infrastructure.DesertTomb.Monsters.Creatures;

internal sealed class TombAssassinBug : Bug
{
    internal TombAssassinBug(
        MonsterMultipliers multipliers,
        Vector2 position,
        int difficulty = 3,
        string textureName = "Assassin Bug",
        int speedToAdd = 0,
        string? customDropData = null
    )
        : base(position, Game1.random.Next(4), textureName)
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

        TombMonsterBuilder.SetMonsterNames(this, "Assassin Bug");
    }
}
