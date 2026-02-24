using System;
using System.Collections.Generic;
using StardewModdingAPI;
using EggsInWild.DesertTomb.Core.Model.Loot;
using Mikew.NestCore.Helpers.Logging;
using EggsInWild.Infrastructure.ModData;

namespace EggsInWild.Infrastructure.DesertTomb.Loot.Definitions;

/// <summary>
/// Loads DesertTomb drop tables from JSON.
/// </summary>
public sealed class TombDropTables : ITombDropTables
{
    private readonly IReadOnlyDictionary<string, TombDropTableDefinition> _tables;

    public TombDropTables(IJsonFileReader reader, ILog log)
    {
        if (reader is null)
            throw new ArgumentNullException(nameof(reader));
        if (log is null)
            throw new ArgumentNullException(nameof(log));

        _tables = Load(reader, log);
    }

    public bool TryGet(string dropTableId, out TombDropTableDefinition table)
        => _tables.TryGetValue(dropTableId, out table!);

    private static IReadOnlyDictionary<string, TombDropTableDefinition> Load(IJsonFileReader reader, ILog log)
    {
        var raw = reader.ReadJsonFile<Dictionary<string, TombDropTableDefinitionJson>>(DesertTombLootPaths.DropTables);
        if (raw is null || raw.Count == 0)
        {
            log.Log($"[DesertTomb] Missing or empty {DesertTombLootPaths.DropTables}; JSON drop tables disabled.", LogLevel.Warn);
            return new Dictionary<string, TombDropTableDefinition>(StringComparer.OrdinalIgnoreCase);
        }

        var result = new Dictionary<string, TombDropTableDefinition>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in raw)
        {
            var id = (key ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(id))
            {
                log.Log("[DesertTomb] DropTables contains an empty key; skipping.", LogLevel.Warn);
                continue;
            }

            if (value is null)
            {
                log.Log($"[DesertTomb] DropTables '{id}' is null; skipping.", LogLevel.Warn);
                continue;
            }

            if (result.ContainsKey(id))
            {
                log.Log($"[DesertTomb] Duplicate DropTables entry for '{id}'; using first.", LogLevel.Warn);
                continue;
            }

            result[id] = value.ToModel(id);
        }

        return result;
    }
}
