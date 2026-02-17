using Microsoft.Xna.Framework;
using StardewValley.Monsters;
using WildEggsHatching.DesertTomb.Core.Configuration;
using WildEggsHatching.Infrastructure.DesertTomb.Monsters.Base;

namespace WildEggsHatching.Infrastructure.DesertTomb.Monsters.Creatures;

internal sealed class TombHauntedSkull : Bat
{
    internal TombHauntedSkull(
        MonsterMultipliers multipliers,
        Vector2 position,
        int difficulty = 3,
        string textureName = "Fly",
        int speedToAdd = 0,
        string? customDropData = null
    )
        : base(position, 77377)
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

        TombMonsterBuilder.SetMonsterNames(this, "Haunted Skull");
    }
}
