using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Monsters;
using WildEggsHatching.DesertTomb.Spawning;
using WildEggsHatching.DesertTomb.Configuration;
using WildEggsHatching.DesertTomb.Monsters.Base;
using WildEggsHatching.DesertTomb.Utilities;
using static WildEggsHatching.EnumsAndStrings.Enums;

namespace WildEggsHatching.DesertTomb.Monsters.Creatures
{
    internal class TombMummy : Mummy
    {
        internal TombMummy(
            MonsterMultipliers monsterMultipliers,
            Vector2 position,
            int difficulty = 3,
            string textureName = "Mummy",
            int speedToAdd = 0
        )
            : base(position)
        {
            TombMonsterBuilder.SetupMonsterWithDoubleDrops(
                this,
                monsterMultipliers,
                difficulty,
                textureName,
                speedToAdd
            );

            // Add custom mummy items
            objectsToDrop.AddRange(GetAdditionalMummyItems());

            TombMonsterBuilder.SetMonsterNames(this, "Mummy");
        }

        private static List<string> GetAdditionalMummyItems()
        {
            var hats = new string[] { "(H)64", "(H)65" };
            var baseItems =
                $"580 .005 581 .005 582 .005 583 .005 {hats[Game1.random.Next(hats.Length)]} .001";
            var mummyItems = MonsterItemHelper.GetModifiedMonsterItems(
                TombMonsterType.Mummy,
                baseItems
            );

            // Add a prefix to the hat items
            for (int i = 0; i < mummyItems.Count; i++)
            {
                if (mummyItems[i].StartsWith("(O)(H)"))
                {
                    mummyItems[i] = mummyItems[i].Replace("(O)", "");
                }
            }
            return mummyItems;
        }
    }
}
