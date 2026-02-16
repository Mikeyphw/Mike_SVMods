using Microsoft.Xna.Framework;
using StardewValley.Monsters;
using WildEggsHatching.DesertTomb.Configuration;
using WildEggsHatching.DesertTomb.Monsters.Base;

namespace WildEggsHatching.DesertTomb.Monsters.Creatures
{
    internal class TombHauntedSkull : Bat
    {
        internal TombHauntedSkull(
            MonsterMultipliers monsterMultipliers,
            Vector2 position,
            int difficulty = 3,
            string textureName = "Haunted Skull",
            int speedToAdd = 0
        )
            : base(position, 77377)
        {
            TombMonsterBuilder.SetupBasicMonster(this, difficulty, textureName, speedToAdd);

            TombMonsterBuilder.SetMonsterNames(this, "Haunted Skull");
        }
    }
}
