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

            if (!normalized.TryGetValue(canonical, out var existing))
            {
                normalized[canonical] = ovValue;
                continue;
            }

            normalized[canonical] = MergeOverrides(existing, ovValue, canonical);
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
            => ov.Enabled is null && ov.LargeEgg is null && ov.Price is null && ov.IsMagic is null;

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
        }
    }
}
