using Microsoft.Xna.Framework;
using StardewValley.Monsters;
using WildEggsHatching.DesertTomb.Configuration;
using WildEggsHatching.DesertTomb.TombMonsters.Base;

namespace WildEggsHatching.DesertTomb.TombMonsters.Creatures
{
    internal class TombGhost : Ghost
    {
        internal TombGhost(
            MonsterMultipliers monsterMultipliers,
            Vector2 position,
            int difficulty = 2,
            string textureName = "Ghost",
            int speedToAdd = 0
        )
            : base(position)
        {
            TombMonsterBuilder.SetupBasicMonster(
                this,
                difficulty,
                textureName,
                speedToAdd,
                "164 0.016"
            );

            TombMonsterBuilder.SetMonsterNames(this, "Ghost");
        }
    }
}
