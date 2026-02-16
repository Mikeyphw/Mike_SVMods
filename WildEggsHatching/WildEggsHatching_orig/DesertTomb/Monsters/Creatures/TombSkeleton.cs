using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Monsters;
using WildEggsHatching.DesertTomb.Spawning;
using WildEggsHatching.DesertTomb.Configuration;
using WildEggsHatching.DesertTomb.Monsters.Base;

namespace WildEggsHatching.DesertTomb.Monsters.Creatures
{
    internal class TombSkeleton : Skeleton
    {
        internal TombSkeleton(
            MonsterMultipliers monsterMultipliers,
            Vector2 position,
            int difficulty = 3,
            string textureName = "Skeleton",
            int speedToAdd = 0
        )
            : base(position, false)
        {
            TombMonsterBuilder.SetupMonsterWithDoubleDrops(
                this,
                monsterMultipliers,
                difficulty,
                textureName,
                speedToAdd
            );

            TombMonsterBuilder.SetMonsterNames(this, "Skeleton");
        }
    }
}
