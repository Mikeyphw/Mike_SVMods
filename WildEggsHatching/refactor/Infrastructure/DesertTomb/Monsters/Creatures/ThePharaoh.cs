using Microsoft.Xna.Framework;
using StardewValley.Monsters;
using WildEggsHatching.DesertTomb.Core.Configuration;
using WildEggsHatching.Infrastructure.DesertTomb.Monsters.Base;

namespace WildEggsHatching.Infrastructure.DesertTomb.Monsters.Creatures;

/// <summary>
/// Placeholder Pharaoh implementation for the refactor port.
/// The legacy boss has heavy integration hooks and will be ported separately.
/// </summary>
internal sealed class ThePharaoh : Mummy
{
    internal ThePharaoh(
        MonsterMultipliers multipliers,
        Vector2 position,
        int difficulty = 5,
        string textureName = "Mummy_dangerous",
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

        TombMonsterBuilder.SetMonsterNames(this, "Pharaoh");
    }
}
