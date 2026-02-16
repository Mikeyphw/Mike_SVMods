using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Monsters;
using WildEggsHatching.DesertTomb.Core.Configuration;
using WildEggsHatching.Infrastructure.DesertTomb.Loot;
using WildEggsHatching.Infrastructure.DesertTomb.Monsters.Base;
using static WildEggsHatching.DesertTomb.Core.Enums.DesertTombEnums;

namespace WildEggsHatching.Infrastructure.DesertTomb.Monsters.Creatures;

internal sealed class TombMummy : Mummy
{
    internal TombMummy(
        MonsterMultipliers multipliers,
        Vector2 position,
        int difficulty = 3,
        string textureName = "Mummy",
        int speedToAdd = 0,
        string? customDropData = null
    )
        : base(position)
    {
        TombMonsterBuilder.SetupMonsterWithDoubleDrops(
            this,
            multipliers,
            difficulty,
            textureName,
            speedToAdd,
            luckyBonusEnabled: true,
            customDropData: customDropData
        );

        objectsToDrop.AddRange(GetAdditionalMummyItems());

        TombMonsterBuilder.SetMonsterNames(this, "Mummy");
    }

    private static List<string> GetAdditionalMummyItems()
    {
        var hats = new[] { "(H)64", "(H)65" };
        var baseItems =
            $"580 .005 581 .005 582 .005 583 .005 {hats[Game1.random.Next(hats.Length)]} .001";

        var mummyItems = MonsterItemHelper.GetModifiedMonsterItems(
            TombMonsterType.Mummy,
            luckyBonusEnabled: true,
            extraItems: baseItems
        );

        // Legacy formatting fix: remove "(O)" prefix if "(O)(H)" shows up.
        for (var i = 0; i < mummyItems.Count; i++)
        {
            if (mummyItems[i].StartsWith("(O)(H)"))
                mummyItems[i] = mummyItems[i].Replace("(O)", "");
        }

        return mummyItems;
    }
}
