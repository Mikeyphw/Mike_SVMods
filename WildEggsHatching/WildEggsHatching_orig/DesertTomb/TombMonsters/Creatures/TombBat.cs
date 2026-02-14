using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Monsters;
using WildEggsHatching.DesertTomb.Calculation;
using WildEggsHatching.DesertTomb.Configuration;
using WildEggsHatching.DesertTomb.TombMonsters.Base;

namespace WildEggsHatching.DesertTomb.TombMonsters.Creatures
{
    internal class TombBat : Bat
    {
        internal TombBat(
            MonsterMultipliers monsterMultipliers,
            Vector2 position,
            int mineLevel,
            int difficulty = 3,
            string textureName = "Bat_dangerous",
            int speedToAdd = 0
        )
            : base(position, mineLevel)
        {
            TombMonsterBuilder.SetupMonsterWithDoubleDrops(
                this,
                monsterMultipliers,
                difficulty,
                textureName,
                speedToAdd
            );

            TombMonsterBuilder.SetMonsterNames(this, "Bat");
        }
    }
}
