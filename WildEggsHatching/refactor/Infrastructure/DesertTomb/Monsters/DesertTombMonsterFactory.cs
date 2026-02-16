using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley.Monsters;
using WildEggsHatching.DesertTomb.Core.Configuration;
using WildEggsHatching.Infrastructure.DesertTomb.Monsters.Creatures;
using static WildEggsHatching.DesertTomb.Core.Enums.DesertTombEnums;

namespace WildEggsHatching.Infrastructure.DesertTomb.Monsters;

/// <summary>
/// Default DesertTomb monster factory (ported from the legacy SpawnRateCalculator monster factory).
/// </summary>
public sealed class DesertTombMonsterFactory : IDesertTombMonsterFactory
{
    private static readonly IReadOnlyDictionary<TombMonsterType, string> TextureNames = new Dictionary<TombMonsterType, string>
    {
        { TombMonsterType.AssassinBug, "Assassin Bug" },
        { TombMonsterType.Bat, "Bat_dangerous" },
        { TombMonsterType.BruteMummy, "Brute Mummy" },
        { TombMonsterType.Duggy, "Sandy Duggy" },
        { TombMonsterType.HauntedSkull, "Fly" },
        { TombMonsterType.Ghost, "Carbon Ghost" },
        { TombMonsterType.Mummy, "Mummy" },
        { TombMonsterType.RoyalSerpent, "Royal Serpent" },
        { TombMonsterType.Skeleton, "Skeleton" },
        { TombMonsterType.SkeletonMage, "Skeleton Mage" },
        { TombMonsterType.Spider, "Spider" },
        { TombMonsterType.TheTombPharaoh, "ThePharaoh" },
        { TombMonsterType.Crawler, "Crawler" },
        { TombMonsterType.Matango, "Matango" },
        { TombMonsterType.Hurricane, "Hurricane" },
        { TombMonsterType.Sandi, "Sandi" },
    };

    public Monster Create(
        TombMonsterType monsterType,
        Vector2 position,
        int difficulty,
        int speedToAdd,
        MonsterMultipliers multipliers,
        string? customDropData = null
    )
    {
        if (multipliers is null)
            throw new ArgumentNullException(nameof(multipliers));

        var texture = TextureNames.TryGetValue(monsterType, out var t) ? t : monsterType.ToString();

        return monsterType switch
        {
            TombMonsterType.AssassinBug => new TombAssassinBug(multipliers, position, difficulty, texture, speedToAdd, customDropData),
            TombMonsterType.Bat => new TombBat(multipliers, position, difficulty, texture, speedToAdd, customDropData),
            TombMonsterType.BruteMummy => new BruteMummy(multipliers, position, difficulty, texture, speedToAdd, customDropData),
            TombMonsterType.Duggy => new TombDuggy(multipliers, position, difficulty, texture, speedToAdd, customDropData),
            TombMonsterType.HauntedSkull => new TombHauntedSkull(multipliers, position, difficulty, texture, speedToAdd, customDropData),
            TombMonsterType.Ghost => new TombGhost(multipliers, position, difficulty, texture, speedToAdd, customDropData),
            TombMonsterType.Mummy => new TombMummy(multipliers, position, difficulty, texture, speedToAdd, customDropData),
            TombMonsterType.RoyalSerpent => new TombSerpent(multipliers, position, difficulty, texture, speedToAdd, customDropData),
            TombMonsterType.Skeleton => new TombSkeleton(multipliers, position, difficulty, texture, speedToAdd, customDropData),
            TombMonsterType.SkeletonMage => new TombSkeletonMage(multipliers, position, difficulty, texture, speedToAdd, customDropData),
            TombMonsterType.Spider => new TombSpider(multipliers, position, difficulty, texture, speedToAdd, customDropData),
            TombMonsterType.TheTombPharaoh => new ThePharaoh(multipliers, position, difficulty, texture, speedToAdd, customDropData),

            TombMonsterType.Crawler => new Crawler(multipliers, position, difficulty, texture, speedToAdd, customDropData),
            TombMonsterType.Matango => new Matango(multipliers, position, difficulty, texture, speedToAdd, customDropData),
            TombMonsterType.Hurricane => new Hurricane(multipliers, position, difficulty, texture, speedToAdd, customDropData),
            TombMonsterType.Sandi => new Sandi(multipliers, position, difficulty, texture, speedToAdd, customDropData),
            _ => new TombMummy(multipliers, position, difficulty, "Mummy", speedToAdd, customDropData),
        };
    }
}
