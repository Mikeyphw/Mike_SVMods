using System;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Monsters;
using WildEggsHatching.DesertTomb.Core.Configuration;
using WildEggsHatching.DesertTomb.Core.Model.Monsters;
using WildEggsHatching.Infrastructure.DesertTomb.Monsters.Creatures;
using WildEggsHatching.Infrastructure.DesertTomb.Monsters.Definitions;
using WildEggsHatching.Infrastructure.DesertTomb.Loot;
using WildEggsHatching.Infrastructure.DesertTomb.Loot.Definitions;
using WildEggsHatching.Infrastructure.Logging;
using static WildEggsHatching.DesertTomb.Core.Enums.DesertTombEnums;

namespace WildEggsHatching.Infrastructure.DesertTomb.Monsters;

/// <summary>
/// DesertTomb monster factory that instantiates a base monster then applies a JSON-driven template.
/// </summary>
public sealed class DesertTombMonsterFactory : IDesertTombMonsterFactory
{
    private readonly ITombMonsterDefinitions _definitions;
    private readonly ITombDropTables _dropTables;
    private readonly TombDropTableEngine _dropEngine;
    private readonly ILog _log;

    public DesertTombMonsterFactory(ITombMonsterDefinitions definitions, ITombDropTables dropTables, TombDropTableEngine dropEngine, ILog log)
    {
        _definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));
        _dropTables = dropTables ?? throw new ArgumentNullException(nameof(dropTables));
        _dropEngine = dropEngine ?? throw new ArgumentNullException(nameof(dropEngine));
        _log = log ?? throw new ArgumentNullException(nameof(log));
    }

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

        var definition = ResolveDefinition(monsterType);

        var effectiveDifficulty = difficulty + definition.Stats.DifficultyAdd;
        var effectiveSpeed = speedToAdd + definition.Stats.SpeedAdd;

        var baseType = ResolveBaseMonsterType(monsterType, definition.BaseMonsterKey);

        var monster = baseType switch
        {
            TombMonsterType.AssassinBug => new TombAssassinBug(multipliers, position, effectiveDifficulty, definition.TextureName, effectiveSpeed, customDropData),
            TombMonsterType.Bat => new TombBat(multipliers, position, effectiveDifficulty, definition.TextureName, effectiveSpeed, customDropData),
            TombMonsterType.BruteMummy => new BruteMummy(multipliers, position, effectiveDifficulty, definition.TextureName, effectiveSpeed, customDropData),
            TombMonsterType.Duggy => new TombDuggy(multipliers, position, effectiveDifficulty, definition.TextureName, effectiveSpeed, customDropData),
            TombMonsterType.HauntedSkull => new TombHauntedSkull(multipliers, position, effectiveDifficulty, definition.TextureName, effectiveSpeed, customDropData),
            TombMonsterType.Ghost => new TombGhost(multipliers, position, effectiveDifficulty, definition.TextureName, effectiveSpeed, customDropData),
            TombMonsterType.Mummy => new TombMummy(multipliers, position, effectiveDifficulty, definition.TextureName, effectiveSpeed, customDropData),
            TombMonsterType.RoyalSerpent => new TombSerpent(multipliers, position, effectiveDifficulty, definition.TextureName, effectiveSpeed, customDropData),
            TombMonsterType.Skeleton => new TombSkeleton(multipliers, position, effectiveDifficulty, definition.TextureName, effectiveSpeed, customDropData),
            TombMonsterType.SkeletonMage => new TombSkeletonMage(multipliers, position, effectiveDifficulty, definition.TextureName, effectiveSpeed, customDropData),
            TombMonsterType.Spider => new TombSpider(multipliers, position, effectiveDifficulty, definition.TextureName, effectiveSpeed, customDropData),
            TombMonsterType.TheTombPharaoh => new ThePharaoh(multipliers, position, effectiveDifficulty, definition.TextureName, effectiveSpeed, customDropData),

            TombMonsterType.Crawler => new Crawler(multipliers, position, effectiveDifficulty, definition.TextureName, effectiveSpeed, customDropData),
            TombMonsterType.Matango => new Matango(multipliers, position, effectiveDifficulty, definition.TextureName, effectiveSpeed, customDropData),
            TombMonsterType.Hurricane => new Hurricane(multipliers, position, effectiveDifficulty, definition.TextureName, effectiveSpeed, customDropData),
            TombMonsterType.Sandi => new Sandi(multipliers, position, effectiveDifficulty, definition.TextureName, effectiveSpeed, customDropData),

            _ => new TombMummy(multipliers, position, effectiveDifficulty, "Mummy", effectiveSpeed, customDropData),
        };

        ApplyTemplate(monster, monsterType, definition);
        ApplyDropOverrides(monster, definition, multipliers, customDropData);

        return monster;
    }

    private TombMonsterDefinition ResolveDefinition(TombMonsterType monsterType)
    {
        if (_definitions.TryGet(monsterType, out var d))
            return d;

        _log.Log($"[DesertTomb] Missing monster definition for {monsterType}; using defaults.", LogLevel.Warn);
        return TombMonsterDefinitionDefaults.Build()[monsterType];
    }

    private static TombMonsterType ResolveBaseMonsterType(TombMonsterType fallback, string baseMonsterKey)
        => Enum.TryParse<TombMonsterType>(baseMonsterKey, ignoreCase: true, out var parsed) ? parsed : fallback;

    private static void ApplyTemplate(Monster monster, TombMonsterType requestedType, TombMonsterDefinition definition)
    {
        // Persist template metadata for later phases (roles/enforcement/drops).
        monster.modData[DesertTombMonsterModDataKeys.MonsterDefinitionId] = definition.DefinitionId;
        monster.modData[DesertTombMonsterModDataKeys.MonsterBaseKey] = definition.BaseMonsterKey;
        monster.modData[DesertTombMonsterModDataKeys.Flags] = $"{(definition.Flags.IsBoss ? 1 : 0)},{(definition.Flags.HostAuthoritative ? 1 : 0)}";

        if (!string.IsNullOrWhiteSpace(definition.DropTableId))
            monster.modData[DesertTombMonsterModDataKeys.DropTableId] = definition.DropTableId!;

        // Ensure the visible name matches the template even if a prior constructor hardcodes names.
        var baseName = string.IsNullOrWhiteSpace(definition.BaseName) ? requestedType.ToString() : definition.BaseName;
        monster.Name = $"Tomb {baseName}";
        monster.displayName = $"Tomb {baseName}";

        ApplyStatOverrides(monster, definition.Stats);
    }

    
    private void ApplyDropOverrides(
        Monster monster,
        TombMonsterDefinition definition,
        MonsterMultipliers multipliers,
        string? customDropData
    )
    {
        if (monster is null)
            throw new ArgumentNullException(nameof(monster));
        if (definition is null)
            throw new ArgumentNullException(nameof(definition));
        if (multipliers is null)
            throw new ArgumentNullException(nameof(multipliers));

        // Phase 6: JSON-only drops. Always clear any prior constructor drops.
        monster.objectsToDrop.Clear();

        if (string.IsNullOrWhiteSpace(definition.DropTableId))
        {
            _log.Log($"[DesertTomb] Monster '{definition.DefinitionId}' has no DropTableId; dropping nothing.", LogLevel.Error);
            return;
        }

        if (!_dropTables.TryGet(definition.DropTableId!, out var table))
        {
            _log.Log($"[DesertTomb] Missing drop table '{definition.DropTableId}' for monster '{definition.DefinitionId}'; dropping nothing.", LogLevel.Error);
            return;
        }

        var rng = Game1.random;
        var luck = TombDropTableEngine.FromPlayer();

        monster.objectsToDrop.AddRange(_dropEngine.RollToItemIds(table, rng, luck));

        if (rng.NextDouble() < multipliers.ChanceToDoubleDropItem)
            monster.objectsToDrop.AddRange(_dropEngine.RollToItemIds(table, rng, luck));
    }


private static void ApplyStatOverrides(Monster monster, TombMonsterStatProfile stats)
    {
        if (stats.MaxHealthMultiplier is float mhMult)
        {
            monster.MaxHealth = Math.Max(1, (int)(monster.MaxHealth * mhMult));
            monster.Health = monster.MaxHealth;
        }

        if (stats.MaxHealthAdd is int mhAdd && mhAdd != 0)
        {
            monster.MaxHealth = Math.Max(1, monster.MaxHealth + mhAdd);
            monster.Health = monster.MaxHealth;
        }

        if (stats.DamageToFarmer is int dmg)
            monster.DamageToFarmer = dmg;

        if (stats.ResilienceAdd is int rAdd && rAdd != 0)
            monster.resilience.Value = Math.Max(0, monster.resilience.Value + rAdd);
    }
}
