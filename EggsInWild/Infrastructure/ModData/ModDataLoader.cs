using System;
using System.Collections.Generic;
using EggsInWild.Infrastructure.Config;
using Mikew.NestCore.Helpers.Config;
using Mikew.NestCore.Helpers.Logging;
using EggsInWild.Infrastructure.ModData.Models;
using LogScopedHelper = Mikew.NestCore.Helpers.Logging.LogScopedHelper;
using static Mikew.NestCore.Helpers.Logging.ScopedLogCatalog;

namespace EggsInWild.Infrastructure.ModData;

public sealed class ModDataLoader : IModDataLoader
{
    private readonly IJsonFileReader _reader;
    private readonly ILog _log;
    public ModDataLoader(IJsonFileReader reader, ILog log)
    {
        _reader = reader;
        _log = log;
    }

    public ModDataSnapshot Load()
    {
        var wildAnimals = ReadOrDefault<Dictionary<string, WildAnimalJson>>(
            ModDataPaths.WildAnimalsList,
            () => new Dictionary<string, WildAnimalJson>());

        var spawnAreas = ReadOrDefault<Dictionary<string, List<SpawnAreaRuleJson>>>(
            ModDataPaths.SpawnAreas,
            () => new Dictionary<string, List<SpawnAreaRuleJson>>());

        var spawnParameters = ReadOrDefault(
            ModDataPaths.SpawnParameters,
            () => new SpawnParametersJson());

        var locationPacks = ReadOrDefault<Dictionary<string, LocationPackJson>>(
            ModDataPaths.LocationPacks,
            () => new Dictionary<string, LocationPackJson>());

        Validate(wildAnimals, spawnAreas, spawnParameters, locationPacks);

        return new ModDataSnapshot(
            WildAnimals: wildAnimals,
            SpawnAreas: spawnAreas,
            SpawnParameters: spawnParameters,
            LocationPacks: locationPacks);
    }

    private T ReadOrDefault<T>(string path, Func<T> fallback) where T : class
    {
#if WEGG_DEV
        try
        {
            var model = _reader.ReadJsonFile<T>(path);
            if (model is not null)
                return model;

            LogScopedHelper.Log(_log, ScopedLogCatalog.ModWeCategory.SaveData, $"Mod data missing: {path}. Using defaults.", StardewModdingAPI.LogLevel.Warn);
            return fallback();
        }
        catch (Exception ex)
        {
            LogScopedHelper.Log(_log, ScopedLogCatalog.ModWeCategory.SaveData, $"Failed reading mod data: {path}. Using defaults. {ex}", StardewModdingAPI.LogLevel.Error);
            return fallback();
        }
#else
        try
        {
            var model = _reader.ReadJsonFile<T>(path);
            if (model is null)
                throw new InvalidOperationException($"Required mod data missing: {path}. Release builds embed JSON; check @(MikeEmbedText) and build targets.");

            return model;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed reading required mod data: {path}. Release builds embed JSON.", ex);
        }
#endif
    }
private void Validate(
        IDictionary<string, WildAnimalJson> wildAnimals,
        IDictionary<string, List<SpawnAreaRuleJson>> spawnAreas,
        SpawnParametersJson spawnParameters,
        IDictionary<string, LocationPackJson> locationPacks)
    {
        // Lightweight Phase 1 validation: ensure non-null collections and basic defaults.
        foreach (var (key, wa) in wildAnimals)
        {
            wa.LocationTypes ??= new Dictionary<string, bool>();
            wa.ExtraModAnimalKeys ??= new Dictionary<string, ExtraModAnimalKeysJson>();

            if (wa.LocationTypes.Count == 0)
                LogScopedHelper.Log(_log, ScopedLogCatalog.ModWeCategory.SaveData, $"WildAnimalsList '{key}' has no LocationTypes; defaulting all to false.", StardewModdingAPI.LogLevel.Trace);

            if (wa.ExtraModAnimalKeys.Count == 0)
                LogScopedHelper.Log(_log, ScopedLogCatalog.ModWeCategory.SaveData, $"WildAnimalsList '{key}' has no ExtraModAnimalKeys; it may never resolve to a farm animal.", StardewModdingAPI.LogLevel.Trace);

            if (string.IsNullOrWhiteSpace(key))
                LogScopedHelper.Log(_log, ScopedLogCatalog.ModWeCategory.SaveData, "WildAnimalsList contains an empty key.", StardewModdingAPI.LogLevel.Warn);
        }

        foreach (var (areaKey, rules) in spawnAreas)
        {
            if (rules is null)
            {
                spawnAreas[areaKey] = new List<SpawnAreaRuleJson>();
                LogScopedHelper.Log(_log, ScopedLogCatalog.ModWeCategory.SaveData, $"SpawnAreas '{areaKey}' had null rules list; defaulted to empty.", StardewModdingAPI.LogLevel.Warn);
                continue;
            }

            foreach (var rule in rules)
            {
                rule.IncludeTerrainTypes ??= new List<string>();
                rule.ExcludeTerrainTypes ??= new List<string>();
                rule.IncludeCoordinates ??= new List<string>();
                rule.ExcludeCoordinates ??= new List<string>();
                rule.ExtraConditions ??= new ExtraConditionsJson();
                rule.ExtraConditions.Seasons ??= new List<string>();
                rule.ExtraConditions.Days ??= new List<int>();
                rule.ExtraConditions.WeatherToday ??= new List<string>();
                rule.ExtraConditions.WeatherTomorrow ??= new List<string>();
                rule.ExtraConditions.WeatherYesterday ??= new List<string>();
            }
        }

        spawnParameters.MaxWorldEggs ??= new Dictionary<string, int>();
        spawnParameters.Parameters_Packs ??= new ParameterPackJson();
        spawnParameters.Parameters_Vanilla ??= new ParameterPackJson();

        foreach (var (_, pack) in locationPacks)
        {
            pack.Locations ??= new Dictionary<string, List<string>>();
        }
    }
}

public sealed class ConfiguredModDataLoader : IModDataLoader
{
    private readonly IModDataLoader _inner;
    private readonly SmapiConfigProvider<ModConfig> _config;
    private readonly ILog _log;

    public ConfiguredModDataLoader(IModDataLoader inner, SmapiConfigProvider<ModConfig> config, ILog log)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _log = log ?? throw new ArgumentNullException(nameof(log));
    }

    public ModDataSnapshot Load()
    {
        var snapshot = _inner.Load();

        var cfg = _config.Get();
        var changed = NormalizeWildAnimalOverrides(snapshot.WildAnimals, cfg.WildAnimalOverrides);

        if (changed)
            _config.Save();

        ApplyWildAnimalOverrides(snapshot.WildAnimals, cfg.WildAnimalOverrides);
        return snapshot;
    }

    
    private bool NormalizeWildAnimalOverrides(
        IReadOnlyDictionary<string, WildAnimalJson> wildAnimals,
        IDictionary<string, WildAnimalOverrideConfig> overrides)
    {
        if (overrides is null || overrides.Count == 0)
            return false;

        var canonicalKeys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var key in wildAnimals.Keys)
        {
            if (string.IsNullOrWhiteSpace(key))
                continue;

            if (!canonicalKeys.TryAdd(key, key))
            {
                LogScopedHelper.Log(
                    _log,
                    ScopedLogCatalog.ModWeCategory.SaveData,
                    $"WildAnimalsList contains duplicate keys differing only by case: '{canonicalKeys[key]}' and '{key}'.",
                    StardewModdingAPI.LogLevel.Warn);
            }
        }

        var entries = new List<(string Key, WildAnimalOverrideConfig? Override)>();
        foreach (var kvp in overrides)
            entries.Add((kvp.Key, kvp.Value));

        var normalized = new Dictionary<string, WildAnimalOverrideConfig>(StringComparer.Ordinal);
        var removedUnknown = new List<string>();
        var removedEmpty = new List<string>();
        var merged = new List<string>();

        bool changed = false;

        foreach (var (rawKey, ov) in entries)
        {
            var trimmedKey = rawKey?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(trimmedKey))
            {
                changed = true;
                continue;
            }

            if (ov is null)
            {
                removedEmpty.Add(trimmedKey);
                changed = true;
                continue;
            }

            if (IsEmpty(ov))
            {
                removedEmpty.Add(trimmedKey);
                changed = true;
                continue;
            }

            var ovValue = ov;

            if (!canonicalKeys.TryGetValue(trimmedKey, out var canonical))
            {
                removedUnknown.Add(trimmedKey);
                changed = true;
                continue;
            }

            if (!string.Equals(trimmedKey, rawKey, StringComparison.Ordinal) || !string.Equals(canonical, trimmedKey, StringComparison.Ordinal))
                changed = true;

            if (wildAnimals.TryGetValue(canonical, out var baseWa) && baseWa is not null)
            {
                if (NormalizeOverrideFields(ovValue, baseWa, canonical))
                    changed = true;

                if (IsEmpty(ovValue))
                {
                    removedEmpty.Add(canonical);
                    changed = true;
                    continue;
                }
            }

            if (!normalized.TryGetValue(canonical, out var existing))
            {
                normalized[canonical] = ovValue;
                continue;
            }

            var mergedOv = MergeOverrides(existing, ovValue, canonical);
            if (wildAnimals.TryGetValue(canonical, out var baseWaMerged) && baseWaMerged is not null)
            {
                if (NormalizeOverrideFields(mergedOv, baseWaMerged, canonical))
                    changed = true;

                if (IsEmpty(mergedOv))
                {
                    removedEmpty.Add(canonical);
                    normalized.Remove(canonical);
                    changed = true;
                    continue;
                }
            }

            normalized[canonical] = mergedOv;
            merged.Add(canonical);
            changed = true;
        }

        if (removedUnknown.Count > 0)
        {
            removedUnknown.Sort(StringComparer.OrdinalIgnoreCase);
            LogScopedHelper.Log(
                _log,
                ScopedLogCatalog.ModWeCategory.SaveData,
                $"Removed unknown WildAnimalOverrides keys: {string.Join(", ", removedUnknown)}",
                StardewModdingAPI.LogLevel.Trace);
        }

        if (removedEmpty.Count > 0)
        {
            removedEmpty.Sort(StringComparer.OrdinalIgnoreCase);
            LogScopedHelper.Log(
                _log,
                ScopedLogCatalog.ModWeCategory.SaveData,
                $"Removed empty WildAnimalOverrides entries (no override fields set): {string.Join(", ", removedEmpty)}",
                StardewModdingAPI.LogLevel.Trace);
        }

        if (merged.Count > 0)
        {
            merged.Sort(StringComparer.OrdinalIgnoreCase);
            LogScopedHelper.Log(
                _log,
                ScopedLogCatalog.ModWeCategory.SaveData,
                $"Merged duplicate WildAnimalOverrides entries: {string.Join(", ", merged)}",
                StardewModdingAPI.LogLevel.Trace);
        }

        if (!changed)
            return false;

        overrides.Clear();
        foreach (var kvp in normalized)
            overrides[kvp.Key] = kvp.Value;

        return true;

        static bool IsEmpty(WildAnimalOverrideConfig ov)
            => ov.Enabled is null
                && ov.LargeEgg is null
                && ov.Price is null
                && ov.IsMagic is null
                && (ov.LocationTypes is null || ov.LocationTypes.Count == 0)
                && (ov.ExtraModAnimalKeys is null || ov.ExtraModAnimalKeys.Count == 0);

        bool NormalizeOverrideFields(WildAnimalOverrideConfig ov, WildAnimalJson baseWa, string wildAnimalKey)
        {
            var changed = false;

            changed |= NormalizeLocationTypesOverride(ov, baseWa, wildAnimalKey);
            changed |= NormalizeExtraModAnimalKeysOverride(ov, baseWa, wildAnimalKey);

            return changed;
        }

        bool NormalizeLocationTypesOverride(WildAnimalOverrideConfig ov, WildAnimalJson baseWa, string wildAnimalKey)
        {
            var raw = ov.LocationTypes;
            if (raw is null)
                return false;

            if (raw.Count == 0)
            {
                ov.LocationTypes = null;
                return true;
            }

            var canonical = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var k in baseWa.LocationTypes.Keys)
            {
                if (string.IsNullOrWhiteSpace(k))
                    continue;

                canonical.TryAdd(k, k);
            }

            var normalized = new Dictionary<string, bool>(StringComparer.Ordinal);

            foreach (var (rawKey, allowed) in raw)
            {
                var trimmed = (rawKey ?? string.Empty).Trim();
                if (trimmed.Length == 0)
                    continue;

                var key = canonical.TryGetValue(trimmed, out var c) ? c : trimmed;

                if (baseWa.LocationTypes.TryGetValue(key, out var baseAllowed) && baseAllowed == allowed)
                    continue;

                if (normalized.TryGetValue(key, out var existing) && existing != allowed)
                {
                    LogScopedHelper.Log(
                        _log,
                        ScopedLogCatalog.ModWeCategory.SaveData,
                        $"WildAnimalOverrides '{wildAnimalKey}' has conflicting LocationTypes override for '{key}'; using the later entry.",
                        StardewModdingAPI.LogLevel.Warn);
                }

                normalized[key] = allowed;
            }

            if (normalized.Count == 0)
            {
                var wasNonNull = ov.LocationTypes is not null;
                ov.LocationTypes = null;
                return wasNonNull;
            }

            if (DictionaryEquals(raw, normalized))
                return false;

            ov.LocationTypes = normalized;
            return true;
        }

        bool NormalizeExtraModAnimalKeysOverride(WildAnimalOverrideConfig ov, WildAnimalJson baseWa, string wildAnimalKey)
        {
            var raw = ov.ExtraModAnimalKeys;
            if (raw is null)
                return false;

            if (raw.Count == 0)
            {
                ov.ExtraModAnimalKeys = null;
                return true;
            }

            var canonicalModIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var modId in baseWa.ExtraModAnimalKeys.Keys)
            {
                if (string.IsNullOrWhiteSpace(modId))
                    continue;

                canonicalModIds.TryAdd(modId, modId);
            }

            var normalized = new Dictionary<string, ExtraModAnimalKeysOverrideConfig>(StringComparer.Ordinal);

            foreach (var (rawModId, blockOv) in raw)
            {
                var trimmedModId = (rawModId ?? string.Empty).Trim();
                if (trimmedModId.Length == 0)
                    continue;

                if (blockOv is null)
                    continue;

                var keys = NormalizeStringList(blockOv.FarmAnimalKeys);
                if (keys.Count == 0)
                    continue;

                var modId = canonicalModIds.TryGetValue(trimmedModId, out var c) ? c : trimmedModId;

                if (baseWa.ExtraModAnimalKeys.TryGetValue(modId, out var baseBlock))
                {
                    var baseKeys = baseBlock.GetEffectiveFarmAnimalKeys();
                    if (SequenceEqualIgnoreCase(keys, baseKeys))
                        continue;
                }

                if (normalized.TryGetValue(modId, out var existing) &&
                    existing.FarmAnimalKeys is not null &&
                    !SequenceEqualIgnoreCase(keys, existing.FarmAnimalKeys))
                {
                    LogScopedHelper.Log(
                        _log,
                        ScopedLogCatalog.ModWeCategory.SaveData,
                        $"WildAnimalOverrides '{wildAnimalKey}' has conflicting ExtraModAnimalKeys override for '{modId}'; using the later entry.",
                        StardewModdingAPI.LogLevel.Warn);
                }

                normalized[modId] = new ExtraModAnimalKeysOverrideConfig
                {
                    FarmAnimalKeys = keys,
                };
            }

            if (normalized.Count == 0)
            {
                var wasNonNull = ov.ExtraModAnimalKeys is not null;
                ov.ExtraModAnimalKeys = null;
                return wasNonNull;
            }

            if (ExtraModAnimalKeysEquals(raw, normalized))
                return false;

            ov.ExtraModAnimalKeys = normalized;
            return true;
        }

        static List<string> NormalizeStringList(IEnumerable<string>? raw)
        {
            if (raw is null)
                return new List<string>();

            var outList = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var s in raw)
            {
                var t = (s ?? string.Empty).Trim();
                if (t.Length == 0)
                    continue;

                if (seen.Add(t))
                    outList.Add(t);
            }

            return outList;
        }

        static bool SequenceEqualIgnoreCase(IReadOnlyList<string> a, IReadOnlyList<string> b)
        {
            if (a.Count != b.Count)
                return false;

            for (var i = 0; i < a.Count; i++)
            {
                if (!string.Equals(a[i], b[i], StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            return true;
        }

        static bool DictionaryEquals(Dictionary<string, bool> a, Dictionary<string, bool> b)
        {
            if (a.Count != b.Count)
                return false;

            foreach (var (key, value) in a)
            {
                if (!b.TryGetValue(key, out var other) || other != value)
                    return false;
            }

            return true;
        }

        static bool ExtraModAnimalKeysEquals(
            Dictionary<string, ExtraModAnimalKeysOverrideConfig> a,
            Dictionary<string, ExtraModAnimalKeysOverrideConfig> b)
        {
            if (a.Count != b.Count)
                return false;

            foreach (var (key, blockA) in a)
            {
                if (!b.TryGetValue(key, out var blockB))
                    return false;

                var listA = blockA.FarmAnimalKeys;
                var listB = blockB.FarmAnimalKeys;

                if (listA is null && listB is null)
                    continue;

                if (listA is null || listB is null)
                    return false;

                if (!SequenceEqualIgnoreCase(listA, listB))
                    return false;
            }

            return true;
        }

        Dictionary<string, bool>? MergeLocationTypes(Dictionary<string, bool>? a, Dictionary<string, bool>? b, string wildAnimalKey)
        {
            if (a is null || a.Count == 0)
                return b is null || b.Count == 0 ? null : new Dictionary<string, bool>(b, StringComparer.Ordinal);

            if (b is null || b.Count == 0)
                return new Dictionary<string, bool>(a, StringComparer.Ordinal);

            var merged = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            foreach (var (k, v) in a)
                merged[k] = v;

            foreach (var (k, v) in b)
            {
                if (merged.TryGetValue(k, out var existing) && existing != v)
                {
                    LogScopedHelper.Log(
                        _log,
                        ScopedLogCatalog.ModWeCategory.SaveData,
                        $"WildAnimalOverrides '{wildAnimalKey}' has conflicting LocationTypes override for '{k}'; using the later entry.",
                        StardewModdingAPI.LogLevel.Warn);
                }

                merged[k] = v;
            }

            var outDict = new Dictionary<string, bool>(StringComparer.Ordinal);
            foreach (var (k, v) in merged)
                outDict[k] = v;

            return outDict.Count == 0 ? null : outDict;
        }

        Dictionary<string, ExtraModAnimalKeysOverrideConfig>? MergeExtraModAnimalKeys(
            Dictionary<string, ExtraModAnimalKeysOverrideConfig>? a,
            Dictionary<string, ExtraModAnimalKeysOverrideConfig>? b,
            string wildAnimalKey)
        {
            if (a is null || a.Count == 0)
                return b is null || b.Count == 0 ? null : CloneExtraBlocks(b);

            if (b is null || b.Count == 0)
                return CloneExtraBlocks(a);

            var merged = new Dictionary<string, ExtraModAnimalKeysOverrideConfig>(StringComparer.OrdinalIgnoreCase);
            foreach (var (modId, block) in a)
                merged[modId] = CloneExtraBlock(block);

            foreach (var (modId, block) in b)
            {
                if (merged.TryGetValue(modId, out var existing) &&
                    existing.FarmAnimalKeys is not null &&
                    block.FarmAnimalKeys is not null &&
                    !SequenceEqualIgnoreCase(existing.FarmAnimalKeys, block.FarmAnimalKeys))
                {
                    LogScopedHelper.Log(
                        _log,
                        ScopedLogCatalog.ModWeCategory.SaveData,
                        $"WildAnimalOverrides '{wildAnimalKey}' has conflicting ExtraModAnimalKeys override for '{modId}'; using the later entry.",
                        StardewModdingAPI.LogLevel.Warn);
                }

                merged[modId] = CloneExtraBlock(block);
            }

            var outDict = new Dictionary<string, ExtraModAnimalKeysOverrideConfig>(StringComparer.Ordinal);
            foreach (var (k, v) in merged)
                outDict[k] = v;

            return outDict.Count == 0 ? null : outDict;
        }

        static Dictionary<string, ExtraModAnimalKeysOverrideConfig> CloneExtraBlocks(Dictionary<string, ExtraModAnimalKeysOverrideConfig> src)
        {
            var dict = new Dictionary<string, ExtraModAnimalKeysOverrideConfig>(StringComparer.Ordinal);
            foreach (var (k, v) in src)
                dict[k] = CloneExtraBlock(v);

            return dict;
        }

        static ExtraModAnimalKeysOverrideConfig CloneExtraBlock(ExtraModAnimalKeysOverrideConfig src)
        {
            return new ExtraModAnimalKeysOverrideConfig
            {
                FarmAnimalKeys = src.FarmAnimalKeys is null ? null : new List<string>(src.FarmAnimalKeys),
            };
        }

        WildAnimalOverrideConfig MergeOverrides(WildAnimalOverrideConfig a, WildAnimalOverrideConfig b, string key)
        {
            if (a.Enabled is not null && b.Enabled is not null && a.Enabled.Value != b.Enabled.Value)
                LogScopedHelper.Log(_log, ScopedLogCatalog.ModWeCategory.SaveData, $"WildAnimalOverrides '{key}' has conflicting Enabled values; using the later entry.", StardewModdingAPI.LogLevel.Warn);

            if (a.LargeEgg is not null && b.LargeEgg is not null && a.LargeEgg.Value != b.LargeEgg.Value)
                LogScopedHelper.Log(_log, ScopedLogCatalog.ModWeCategory.SaveData, $"WildAnimalOverrides '{key}' has conflicting LargeEgg values; using the later entry.", StardewModdingAPI.LogLevel.Warn);

            if (a.Price is not null && b.Price is not null && a.Price.Value != b.Price.Value)
                LogScopedHelper.Log(_log, ScopedLogCatalog.ModWeCategory.SaveData, $"WildAnimalOverrides '{key}' has conflicting Price values; using the later entry.", StardewModdingAPI.LogLevel.Warn);

            if (a.IsMagic is not null && b.IsMagic is not null && a.IsMagic.Value != b.IsMagic.Value)
                LogScopedHelper.Log(_log, ScopedLogCatalog.ModWeCategory.SaveData, $"WildAnimalOverrides '{key}' has conflicting IsMagic values; using the later entry.", StardewModdingAPI.LogLevel.Warn);

            return new WildAnimalOverrideConfig
            {
                Enabled = b.Enabled ?? a.Enabled,
                LargeEgg = b.LargeEgg ?? a.LargeEgg,
                Price = b.Price ?? a.Price,
                IsMagic = b.IsMagic ?? a.IsMagic,
                LocationTypes = MergeLocationTypes(a.LocationTypes, b.LocationTypes, key),
                ExtraModAnimalKeys = MergeExtraModAnimalKeys(a.ExtraModAnimalKeys, b.ExtraModAnimalKeys, key),
            };
        }
    }


    private void ApplyWildAnimalOverrides(IReadOnlyDictionary<string, WildAnimalJson> wildAnimals, IDictionary<string, WildAnimalOverrideConfig> overrides)
    {
        if (wildAnimals is null) throw new ArgumentNullException(nameof(wildAnimals));
        if (overrides is null || overrides.Count == 0)
            return;

        foreach (var (key, ov) in overrides)
        {
            if (ov is null)
                continue;

            if (!wildAnimals.TryGetValue(key, out var wa) || wa is null)
                continue;

            if (ov.Enabled is not null)
                wa.Enabled = ov.Enabled.Value;

            if (ov.LargeEgg is not null)
                wa.LargeEgg = ov.LargeEgg.Value;

            if (ov.Price is not null)
                wa.Price = ov.Price.Value;

            if (ov.IsMagic is not null)
                wa.IsMagic = ov.IsMagic.Value;

            if (ov.LocationTypes is { Count: > 0 })
            {
                wa.LocationTypes ??= new Dictionary<string, bool>();
                foreach (var (ltKey, allowed) in ov.LocationTypes)
                    wa.LocationTypes[ltKey] = allowed;
            }

            if (ov.ExtraModAnimalKeys is { Count: > 0 })
            {
                wa.ExtraModAnimalKeys ??= new Dictionary<string, ExtraModAnimalKeysJson>();
                foreach (var (modId, blockOv) in ov.ExtraModAnimalKeys)
                {
                    if (blockOv?.FarmAnimalKeys is not { Count: > 0 })
                        continue;

                    wa.ExtraModAnimalKeys[modId] = new ExtraModAnimalKeysJson
                    {
                        FarmAnimalKeys = new List<string>(blockOv.FarmAnimalKeys),
                    };
                }
            }
        }
    }
}
