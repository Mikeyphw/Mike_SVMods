using System;
using System.Collections.Generic;
using Mikew.NestCore.Helpers.Logging;
using EggsInWild.Infrastructure.ModData.Models;
using SmapiLog = Mikew.NestCore.Helpers.Logging.SmapiLog;

namespace EggsInWild.Infrastructure.ModData;

public sealed class ModDataLoader : IModDataLoader
{
    private readonly IJsonFileReader _reader;
    private readonly ILog _log;

    /// <summary>Helper to emit scoped log messages, falling back to basic log if not a SmapiLog instance.</summary>
    private void LogScoped(string category, string message, StardewModdingAPI.LogLevel level)
    {
        if (_log is SmapiLog slog)
            slog.LogScoped(ScopedLogCatalog.ModWe, category, message, level);
        else
            _log.Log(message, level);
    }

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
        try
        {
            var model = _reader.ReadJsonFile<T>(path);
            if (model is not null)
                return model;

            _log.Log($"Mod data missing: {path}. Using defaults.", StardewModdingAPI.LogLevel.Warn);
            return fallback();
        }
        catch (Exception ex)
        {
            LogScoped("save-data", $"Failed reading mod data: {path}. Using defaults. {ex}", StardewModdingAPI.LogLevel.Error);
            return fallback();
        }
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
                LogScoped("save-data", $"WildAnimalsList '{key}' has no LocationTypes; defaulting all to false.", StardewModdingAPI.LogLevel.Trace);

            if (wa.ExtraModAnimalKeys.Count == 0)
                LogScoped("save-data", $"WildAnimalsList '{key}' has no ExtraModAnimalKeys; it may never resolve to a farm animal.", StardewModdingAPI.LogLevel.Trace);

            if (string.IsNullOrWhiteSpace(key))
                LogScoped("save-data", "WildAnimalsList contains an empty key.", StardewModdingAPI.LogLevel.Warn);
        }

        foreach (var (areaKey, rules) in spawnAreas)
        {
            if (rules is null)
            {
                spawnAreas[areaKey] = new List<SpawnAreaRuleJson>();
                LogScoped("save-data", $"SpawnAreas '{areaKey}' had null rules list; defaulted to empty.", StardewModdingAPI.LogLevel.Warn);
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
