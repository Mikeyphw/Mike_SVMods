using Microsoft.Xna.Framework;
using StardewValley.Monsters;
using EggsInWild.DesertTomb.Core.Configuration;
using static EggsInWild.DesertTomb.Core.Enums.DesertTombEnums;

namespace EggsInWild.Infrastructure.DesertTomb.Monsters;

/// <summary>
/// Creates concrete SDV monster instances for DesertTomb spawns.
/// </summary>
public interface IDesertTombMonsterFactory
{
    Monster Create(
        TombMonsterType monsterType,
        Vector2 position,
        int difficulty,
        int speedToAdd,
        MonsterMultipliers multipliers,
        string? customDropData = null
    );
}
