using Microsoft.Xna.Framework;
using StardewValley.Monsters;
using WildEggsHatching.DesertTomb.Configuration;
using WildEggsHatching.DesertTomb.TombMonsters.Base;

namespace WildEggsHatching.DesertTomb.TombMonsters.Creatures
{
    internal class TombSpider : Leaper
    {
        internal TombSpider(
            MonsterMultipliers monsterMultipliers,
            Vector2 position,
            int difficulty = 3,
            string textureName = "DustSpinner",
            int speedToAdd = 0
        )
            : base(position)
        {
            TombMonsterBuilder.SetupBasicMonster(this, difficulty, textureName, speedToAdd);

            TombMonsterBuilder.SetMonsterNames(this, "Spider");
        }
    }
}
