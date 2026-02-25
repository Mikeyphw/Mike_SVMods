#if WEGG_DEV
using System;
using System.Collections.Generic;
using System.Linq;
using EggsInWild.Infrastructure.DesertTomb.Monsters.Definitions;
using EggsInWild.Infrastructure.ModData;
using EggsInWild.Infrastructure.ModData.Models;
using Mikew.NestCore.Helpers.Logging;
using Mikew.NestCore.Integrations.Gmcm;
using StardewModdingAPI;

namespace EggsInWild.Infrastructure.Config;

/// <summary>
/// Dev-only GMCM editors for JSON source-of-truth files.
/// Writes updates directly to JSON on disk so the JSON remains authoritative.
/// </summary>
internal sealed class DevJsonTuningService
{
    private readonly IModHelper _helper;
    private readonly ILog _log;

    private SpawnParametersJson? _spawnParameters;
    private Dictionary<string, TombMonsterDefinitionJson>? _monsterDefinitions;
    private Dictionary<string, SpawnAreaRuleJson>? _spawnAreas;
    private Dictionary<string, LocationPackJson>? _locationPacks;

    public DevJsonTuningService(IModHelper helper, ILog log)
    {
        _helper = helper ?? throw new ArgumentNullException(nameof(helper));
        _log = log ?? throw new ArgumentNullException(nameof(log));
    }

    public void BuildSpawnParametersPage(GmcmPageBuilder<ModConfig> page)
    {
        EnsureLoaded();
        page.Section("ModData/SpawnParameters.json");

        page.Int(
            optionId: "debug.json.spawn.maxSpawningTries",
            getValue: () => _spawnParameters!.MaxSpawningTries,
            setValue: v => UpdateSpawnParameters(p => p.MaxSpawningTries = Math.Max(0, v)),
            min: 0,
            max: 10000,
            interval: 10
        );

        page.Float(
            optionId: "debug.json.spawn.baseRandomChance",
            getValue: () => (float)_spawnParameters!.BaseRandomChance,
            setValue: v => UpdateSpawnParameters(p => p.BaseRandomChance = Math.Clamp(v, 0f, 1f)),
            min: 0f,
            max: 1f,
            interval: 0.01f
        );

        page.Separator(simple: true);

        page.Section("ModData/SpawnAreas.json");
        foreach (var entry in _spawnAreas)
        {
            var key = entry.Key;
            var area = entry.Value;

            page.Int($"debug.json.spawn.areas.{key}.randomChance",
                getValue: () => area.RandomChance,
                setValue: v => UpdateSpawnAreas(() => area.RandomChance = Math.Clamp(v, 0f, 1f)),
                min: 0f,
                max: 1f,
                interval: 0.01f
            );
        }

        page.Separator(simple: true);

        page.Section("ModData/LocationPacks.json");
        foreach (var pack in _locationPacks)
        {
            var packKey = pack.Key;
            var packValue = pack.Value;

            page.Int($"debug.json.locationPacks.{packKey}.minSpawnsPerDay",
                getValue: () => packValue.MinSpawnsPerDay,
                setValue: v => UpdateLocationPacks(() => packValue.MinSpawnsPerDay = v),
                min: 0,
                max: 100,
                interval: 1
            );
        }
    }

    private void EnsureLoaded()
    {
        if (_spawnParameters is not null && _spawnAreas is not null && _locationPacks is not null)
            return;

        var reader = new SmapiJsonFileReader(_helper);

        _spawnParameters = reader.ReadJsonFile<SpawnParametersJson>(ModDataPaths.SpawnParameters) ?? new SpawnParametersJson();
        _spawnAreas = reader.ReadJsonFile<Dictionary<string, SpawnAreaRuleJson>>(ModDataPaths.SpawnAreas) ?? new Dictionary<string, SpawnAreaRuleJson>();
        _locationPacks = reader.ReadJsonFile<Dictionary<string, LocationPackJson>>(ModDataPaths.LocationPacks) ?? new Dictionary<string, LocationPackJson>();
    }

    private void UpdateSpawnParameters(Action<SpawnParametersJson> update)
    {
        EnsureLoaded();
        update(_spawnParameters!);
        SaveSpawnParameters();
    }

    private void SaveSpawnParameters()
    {
        try
        {
            _helper.Data.WriteJsonFile(ModDataPaths.SpawnParameters, _spawnParameters);
        }
        catch (Exception ex)
        {
            _log.Log($"[DevJson] Failed writing {ModDataPaths.SpawnParameters}: {ex}", LogLevel.Warn);
        }
    }

    private void UpdateSpawnAreas(Action update)
    {
        EnsureLoaded();
        update();
        SaveSpawnAreas();
    }

    private void SaveSpawnAreas()
    {
        try
        {
            _helper.Data.WriteJsonFile(ModDataPaths.SpawnAreas, _spawnAreas);
        }
        catch (Exception ex)
        {
            _log.Log($"[DevJson] Failed writing {ModDataPaths.SpawnAreas}: {ex}", LogLevel.Warn);
        }
    }

    private void UpdateLocationPacks(Action update)
    {
        EnsureLoaded();
        update();
        SaveLocationPacks();
    }

    private void SaveLocationPacks()
    {
        try
        {
            _helper.Data.WriteJsonFile(ModDataPaths.LocationPacks, _locationPacks);
        }
        catch (Exception ex)
        {
            _log.Log($"[DevJson] Failed writing {ModDataPaths.LocationPacks}: {ex}", LogLevel.Warn);
        }
    }
}
#endif
