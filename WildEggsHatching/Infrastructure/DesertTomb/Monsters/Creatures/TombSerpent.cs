using Microsoft.Xna.Framework;
using StardewValley.Monsters;
using WildEggsHatching.DesertTomb.Core.Configuration;
using WildEggsHatching.Infrastructure.DesertTomb.Monsters.Base;

namespace WildEggsHatching.Infrastructure.DesertTomb.Monsters.Creatures;

internal sealed class TombSerpent : Serpent
{
    internal TombSerpent(
        MonsterMultipliers multipliers,
        Vector2 position,
        int difficulty = 3,
        string textureName = "Royal Serpent",
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
            customDropData: customDropData
        );

        TombMonsterBuilder.SetMonsterNames(this, "Serpent");
    }
}
