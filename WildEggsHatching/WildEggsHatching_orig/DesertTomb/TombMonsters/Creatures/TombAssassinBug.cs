using Microsoft.Xna.Framework;
using StardewValley.Monsters;
using WildEggsHatching.DesertTomb.Configuration;
using WildEggsHatching.DesertTomb.TombMonsters.Base;

namespace WildEggsHatching.DesertTomb.TombMonsters.Creatures
{
    internal class TombAssassinBug : Bug
    {
        internal TombAssassinBug(
            MonsterMultipliers monsterMultipliers,
            Vector2 position,
            int facingDirection,
            string textureName,
            int difficulty = 3,
            int speedToAdd = 0
        )
            : base(position, facingDirection, textureName)
        {
            TombMonsterBuilder.SetupBasicMonster(this, difficulty, textureName, speedToAdd);

            TombMonsterBuilder.SetMonsterNames(this, "Assassin Bug");
        }
    }
}
