using System;
using System.Collections.Generic;
using StardewModdingAPI;
using EggsInWild.DesertTomb.Core.Model.Monsters;
using Mikew.NestCore.Helpers.Logging;
using EggsInWild.Infrastructure.ModData;
using static EggsInWild.DesertTomb.Core.Enums.DesertTombEnums;

namespace EggsInWild.Infrastructure.DesertTomb.Monsters.Definitions;

/// <summary>
/// Loads DesertTomb monster definitions from JSON and provides validated templates.
/// </summary>
public sealed class TombMonsterDefinitions : ITombMonsterDefinitions
{
    private readonly IReadOnlyDictionary<TombMonsterType, TombMonsterDefinition> _definitions;

    public TombMonsterDefinitions(IJsonFileReader reader, ILog log)
    {
        if (reader is null)
            throw new ArgumentNullException(nameof(reader));
        if (log is null)
            throw new ArgumentNullException(nameof(log));

        _definitions = Load(reader, log);
    }

    public bool TryGet(TombMonsterType type, out TombMonsterDefinition definition)
        => _definitions.TryGetValue(type, out definition!);

    private static IReadOnlyDictionary<TombMonsterType, TombMonsterDefinition> Load(IJsonFileReader reader, ILog log)
    {
        var raw = reader.ReadJsonFile<Dictionary<string, TombMonsterDefinitionJson>>(DesertTombDefinitionPaths.MonsterDefinitions);
        if (raw is null || raw.Count == 0)
        {
            log.Log($"[DesertTomb] Missing or empty {DesertTombDefinitionPaths.MonsterDefinitions}; using defaults.", LogLevel.Warn);
            return TombMonsterDefinitionDefaults.Build();
        }

        var result = new Dictionary<TombMonsterType, TombMonsterDefinition>();
        foreach (var (key, value) in raw)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                log.Log("[DesertTomb] MonsterDefinitions contains an empty key; skipping.", LogLevel.Warn);
                continue;
            }

            if (!Enum.TryParse<TombMonsterType>(key, ignoreCase: true, out var monsterType))
            {
                log.Log($"[DesertTomb] MonsterDefinitions key '{key}' does not match TombMonsterType; skipping.", LogLevel.Warn);
                continue;
            }

            if (value is null)
            {
                log.Log($"[DesertTomb] MonsterDefinitions '{key}' is null; skipping.", LogLevel.Warn);
                continue;
            }

            var baseMonsterKey = string.IsNullOrWhiteSpace(value.BaseMonster) ? key : value.BaseMonster!.Trim();
            var baseName = string.IsNullOrWhiteSpace(value.BaseName) ? key : value.BaseName!.Trim();
            var texture = string.IsNullOrWhiteSpace(value.Texture) ? key : value.Texture!.Trim();

            var stats = value.Stats?.ToModel() ?? TombMonsterStatProfile.Default;
            var flags = value.Flags?.ToModel() ?? TombMonsterBehaviorFlags.Default;

            var model = new TombMonsterDefinition(
                MonsterType: monsterType,
                DefinitionId: key.Trim(),
                BaseMonsterKey: baseMonsterKey,
                BaseName: baseName,
                TextureName: texture,
                Stats: stats,
                Flags: flags,
                DropTableId: string.IsNullOrWhiteSpace(value.DropTableId) ? null : value.DropTableId!.Trim()
            );

            if (result.ContainsKey(monsterType))
            {
                log.Log($"[DesertTomb] Duplicate MonsterDefinitions entry for {monsterType}; using first.", LogLevel.Warn);
                continue;
            }

            result[monsterType] = model;
        }

        if (result.Count == 0)
        {
            log.Log($"[DesertTomb] MonsterDefinitions had no valid entries; using defaults.", LogLevel.Warn);
            return TombMonsterDefinitionDefaults.Build();
        }

        // Fill gaps with defaults to avoid runtime failures during incremental migration.
        var defaults = TombMonsterDefinitionDefaults.Build();
        foreach (var (type, def) in defaults)
        {
            if (!result.ContainsKey(type))
                result[type] = def;
        }

        return result;
    }
}
