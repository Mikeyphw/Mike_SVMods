using System;
using System.Collections.Generic;
using EggsInWild.Infrastructure.Config;
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
    private readonly Func<ModConfig> _getConfig;
    private readonly ILog _log;

    public ConfiguredModDataLoader(IModDataLoader inner, Func<ModConfig> getConfig, ILog log)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _getConfig = getConfig ?? throw new ArgumentNullException(nameof(getConfig));
        _log = log ?? throw new ArgumentNullException(nameof(log));
    }

    public ModDataSnapshot Load()
    {
        var snapshot = _inner.Load();
        ApplyWildAnimalOverrides(snapshot.WildAnimals, _getConfig().WildAnimalOverrides);
        return snapshot;
    }

    private void ApplyWildAnimalOverrides(IReadOnlyDictionary<string, WildAnimalJson> wildAnimals, IDictionary<string, WildAnimalOverrideConfig> overrides)
    {
        if (wildAnimals is null) throw new ArgumentNullException(nameof(wildAnimals));
        if (overrides is null || overrides.Count == 0)
            return;

        var missing = new List<string>();

        foreach (var (key, ov) in overrides)
        {
            if (string.IsNullOrWhiteSpace(key))
                continue;

            if (!wildAnimals.TryGetValue(key, out var wa) || wa is null)
            {
                missing.Add(key);
                continue;
            }

            if (ov?.Enabled is not null)
                wa.Enabled = ov.Enabled.Value;
        }

        if (missing.Count > 0)
        {
            missing.Sort(StringComparer.OrdinalIgnoreCase);
            LogScopedHelper.Log(
                _log,
                ScopedLogCatalog.ModWeCategory.SaveData,
                $"Config WildAnimalOverrides contains unknown keys: {string.Join(", ", missing)}",
                StardewModdingAPI.LogLevel.Trace);
        }
    }
}
