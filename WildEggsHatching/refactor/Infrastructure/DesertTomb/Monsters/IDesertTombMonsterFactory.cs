using Microsoft.Xna.Framework;
using StardewValley.Monsters;
using WildEggsHatching.DesertTomb.Core.Configuration;
using static WildEggsHatching.DesertTomb.Core.Enums.DesertTombEnums;

namespace WildEggsHatching.Infrastructure.DesertTomb.Monsters;

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
