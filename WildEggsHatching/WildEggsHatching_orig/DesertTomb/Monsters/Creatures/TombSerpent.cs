using Microsoft.Xna.Framework;
using StardewValley.Monsters;
using WildEggsHatching.DesertTomb.Configuration;
using WildEggsHatching.DesertTomb.Monsters.Base;

namespace WildEggsHatching.DesertTomb.Monsters.Creatures
{
    internal class TombSerpent : Serpent
    {
        internal TombSerpent(
            MonsterMultipliers monsterMultipliers,
            Vector2 position,
            int difficulty = 3,
            string textureName = "Serpent",
            int speedToAdd = 0
        )
            : base(position)
        {
            TombMonsterBuilder.SetupBasicMonster(this, difficulty, textureName, speedToAdd);

            TombMonsterBuilder.SetMonsterNames(this, "Serpent");
        }
    }
}
