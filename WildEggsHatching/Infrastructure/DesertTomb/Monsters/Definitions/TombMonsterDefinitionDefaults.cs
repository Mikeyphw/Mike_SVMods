using System.Collections.Generic;
using WildEggsHatching.DesertTomb.Core.Model.Monsters;
using static WildEggsHatching.DesertTomb.Core.Enums.DesertTombEnums;

namespace WildEggsHatching.Infrastructure.DesertTomb.Monsters.Definitions;

internal static class TombMonsterDefinitionDefaults
{
    public static IReadOnlyDictionary<TombMonsterType, TombMonsterDefinition> Build()
        => new Dictionary<TombMonsterType, TombMonsterDefinition>
        {
            { TombMonsterType.AssassinBug, TombMonsterDefinition.CreateDefaults(TombMonsterType.AssassinBug, textureName: "Assassin Bug", baseName: "Assassin Bug") },
            { TombMonsterType.Bat, TombMonsterDefinition.CreateDefaults(TombMonsterType.Bat, textureName: "Bat_dangerous", baseName: "Bat") },
            { TombMonsterType.BruteMummy, TombMonsterDefinition.CreateDefaults(TombMonsterType.BruteMummy, textureName: "Brute Mummy", baseName: "Brute Mummy") },
            { TombMonsterType.Duggy, TombMonsterDefinition.CreateDefaults(TombMonsterType.Duggy, textureName: "Sandy Duggy", baseName: "Duggy") },
            { TombMonsterType.HauntedSkull, TombMonsterDefinition.CreateDefaults(TombMonsterType.HauntedSkull, textureName: "Fly", baseName: "Haunted Skull") },
            { TombMonsterType.Ghost, TombMonsterDefinition.CreateDefaults(TombMonsterType.Ghost, textureName: "Carbon Ghost", baseName: "Ghost") },
            { TombMonsterType.Mummy, TombMonsterDefinition.CreateDefaults(TombMonsterType.Mummy, textureName: "Mummy", baseName: "Mummy") },
            { TombMonsterType.RoyalSerpent, TombMonsterDefinition.CreateDefaults(TombMonsterType.RoyalSerpent, textureName: "Royal Serpent", baseName: "Royal Serpent") },
            { TombMonsterType.Skeleton, TombMonsterDefinition.CreateDefaults(TombMonsterType.Skeleton, textureName: "Skeleton", baseName: "Skeleton") },
            { TombMonsterType.SkeletonMage, TombMonsterDefinition.CreateDefaults(TombMonsterType.SkeletonMage, textureName: "Skeleton Mage", baseName: "Skeleton Mage") },
            { TombMonsterType.Spider, TombMonsterDefinition.CreateDefaults(TombMonsterType.Spider, textureName: "Spider", baseName: "Spider") },
            { TombMonsterType.TheTombPharaoh, TombMonsterDefinition.CreateDefaults(TombMonsterType.TheTombPharaoh, textureName: "ThePharaoh", baseName: "Pharaoh") },

            { TombMonsterType.Crawler, TombMonsterDefinition.CreateDefaults(TombMonsterType.Crawler, textureName: "Crawler", baseName: "Crawler") },
            { TombMonsterType.Matango, TombMonsterDefinition.CreateDefaults(TombMonsterType.Matango, textureName: "Matango", baseName: "Matango") },
            { TombMonsterType.Hurricane, TombMonsterDefinition.CreateDefaults(TombMonsterType.Hurricane, textureName: "Hurricane", baseName: "Hurricane") },
            { TombMonsterType.Sandi, TombMonsterDefinition.CreateDefaults(TombMonsterType.Sandi, textureName: "Sandi", baseName: "Sandi") },
        };
}
