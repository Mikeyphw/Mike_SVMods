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
    private Dictionary<string, List<SpawnAreaRuleJson>>? _spawnAreas;
    private Dictionary<string, LocationPackJson>? _locationPacks;

    public DevJsonTuningService(IModHelper helper, ILog log)
    {
        _helper = helper ?? throw new ArgumentNullException(nameof(helper));
        _log = log ?? throw new ArgumentNullException(nameof(log));
    }

    public void BuildSpawnParametersPage(GmcmPageBuilder<ModConfig> page)
    {
        EnsureLoaded();
        var spawnParameters = _spawnParameters!;
        var spawnAreas = _spawnAreas!;
        var locationPacks = _locationPacks!;

        page.SectionLabel(() => "ModData/SpawnParameters.json");

        page.Int(
            optionId: "debug.json.spawn.maxSpawningTries",
            getValue: () => spawnParameters.MaxSpawningTries,
            setValue: v => UpdateSpawnParameters(p => p.MaxSpawningTries = Math.Max(0, v)),
            min: 0,
            max: 10000,
            interval: 10
        );

        page.Int(
            optionId: "debug.json.spawn.maxEggPerMapAny",
            getValue: () => spawnParameters.MaxEggPerMap_Any,
            setValue: v => UpdateSpawnParameters(p => p.MaxEggPerMap_Any = Math.Max(0, v)),
            min: 0,
            max: 50,
            interval: 1
        );

        page.Int(
            optionId: "debug.json.spawn.maxEggPerMapMagic",
            getValue: () => spawnParameters.MaxEggPerMap_Magic,
            setValue: v => UpdateSpawnParameters(p => p.MaxEggPerMap_Magic = Math.Max(0, v)),
            min: 0,
            max: 50,
            interval: 1
        );

        page.Int(
            optionId: "debug.json.spawn.maxEggPerMapLarge",
            getValue: () => spawnParameters.MaxEggPerMap_Large,
            setValue: v => UpdateSpawnParameters(p => p.MaxEggPerMap_Large = Math.Max(0, v)),
            min: 0,
            max: 50,
            interval: 1
        );

        page.Float(
            optionId: "debug.json.spawn.baseRandomChance",
            getValue: () => (float)spawnParameters.BaseRandomChance,
            setValue: v => UpdateSpawnParameters(p => p.BaseRandomChance = Math.Clamp((double)v, 0.0, 1.0)),
            min: 0f,
            max: 1f,
            interval: 0.01f
        );

        page.Separator(simple: true);
        page.SectionLabel(() => "SpawnParameters – Parameters_Packs");

        var packs = spawnParameters.Parameters_Packs ?? new ParameterPackJson();
        page.Int("debug.json.spawn.packs.minEggArea", () => packs.MinEggArea,
            v => UpdateSpawnParameters(p => p.Parameters_Packs!.MinEggArea = Math.Max(0, v)), min: 0, max: 999, interval: 1);
        page.Int("debug.json.spawn.packs.maxEggArea", () => packs.MaxEggArea,
            v => UpdateSpawnParameters(p => p.Parameters_Packs!.MaxEggArea = Math.Max(0, v)), min: 0, max: 999, interval: 1);
        page.Int("debug.json.spawn.packs.maxEggPack", () => packs.MaxEggPack,
            v => UpdateSpawnParameters(p => p.Parameters_Packs!.MaxEggPack = Math.Max(0, v)), min: 0, max: 999, interval: 1);
        page.Int("debug.json.spawn.packs.certainEggs", () => packs.CertainEggs,
            v => UpdateSpawnParameters(p => p.Parameters_Packs!.CertainEggs = Math.Max(0, v)), min: 0, max: 999, interval: 1);
        page.Bool("debug.json.spawn.packs.useJsonValues", () => packs.UseJsonValues,
            v => UpdateSpawnParameters(p => p.Parameters_Packs!.UseJsonValues = v));
        page.Int("debug.json.spawn.packs.maxEggsAll", () => packs.MaxEggs_All,
            v => UpdateSpawnParameters(p => p.Parameters_Packs!.MaxEggs_All = Math.Max(0, v)), min: 0, max: 999, interval: 1);
        page.Int("debug.json.spawn.packs.minEggs", () => packs.MinEggs,
            v => UpdateSpawnParameters(p => p.Parameters_Packs!.MinEggs = Math.Max(0, v)), min: 0, max: 999, interval: 1);

        page.Separator(simple: true);
        page.SectionLabel(() => "SpawnParameters – Parameters_Vanilla");

        var vanilla = spawnParameters.Parameters_Vanilla ?? new ParameterPackJson();
        page.Int("debug.json.spawn.vanilla.minEggArea", () => vanilla.MinEggArea,
            v => UpdateSpawnParameters(p => p.Parameters_Vanilla!.MinEggArea = Math.Max(0, v)), min: 0, max: 999, interval: 1);
        page.Int("debug.json.spawn.vanilla.maxEggArea", () => vanilla.MaxEggArea,
            v => UpdateSpawnParameters(p => p.Parameters_Vanilla!.MaxEggArea = Math.Max(0, v)), min: 0, max: 999, interval: 1);
        page.Int("debug.json.spawn.vanilla.maxEggPack", () => vanilla.MaxEggPack,
            v => UpdateSpawnParameters(p => p.Parameters_Vanilla!.MaxEggPack = Math.Max(0, v)), min: 0, max: 999, interval: 1);
        page.Int("debug.json.spawn.vanilla.certainEggs", () => vanilla.CertainEggs,
            v => UpdateSpawnParameters(p => p.Parameters_Vanilla!.CertainEggs = Math.Max(0, v)), min: 0, max: 999, interval: 1);
        page.Bool("debug.json.spawn.vanilla.useJsonValues", () => vanilla.UseJsonValues,
            v => UpdateSpawnParameters(p => p.Parameters_Vanilla!.UseJsonValues = v));
        page.Int("debug.json.spawn.vanilla.maxEggsAll", () => vanilla.MaxEggs_All,
            v => UpdateSpawnParameters(p => p.Parameters_Vanilla!.MaxEggs_All = Math.Max(0, v)), min: 0, max: 999, interval: 1);
        page.Int("debug.json.spawn.vanilla.minEggs", () => vanilla.MinEggs,
            v => UpdateSpawnParameters(p => p.Parameters_Vanilla!.MinEggs = Math.Max(0, v)), min: 0, max: 999, interval: 1);

        if (spawnParameters.MaxWorldEggs is { Count: > 0 })
        {
            page.Separator(simple: true);
            page.SectionLabel(() => "SpawnParameters – MaxWorldEggs (existing keys)");

            foreach (var key in spawnParameters.MaxWorldEggs.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase))
            {
                var localKey = key;
                page.Int(
                    optionId: $"debug.json.spawn.maxWorldEggs.{localKey}",
                    getValue: () => spawnParameters.MaxWorldEggs.TryGetValue(localKey, out var value) ? value : 0,
                    setValue: v => UpdateSpawnParameters(p => p.MaxWorldEggs[localKey] = Math.Max(0, v)),
                    min: 0,
                    max: 999,
                    interval: 1
                );
            }

            page.ParagraphDynamic(() => "Note: MaxWorldEggs keys are dynamic; labels use raw keys. Edits write to JSON.");
        }

        page.Separator(simple: true);

        page.SectionLabel(() => "ModData/SpawnAreas.json");
        foreach (var entry in spawnAreas)
        {
            var key = entry.Key;
            var rules = entry.Value ?? new List<SpawnAreaRuleJson>();

            for (var i = 0; i < rules.Count; i++)
            {
                var localIndex = i;
                var area = rules[localIndex];

                page.Float($"debug.json.spawn.areas.{key}.{localIndex}.randomChance",
                    getValue: () => (float)area.RandomChance,
                    setValue: v => UpdateSpawnAreas(() => area.RandomChance = Math.Clamp((double)v, 0.0, 1.0)),
                    min: 0f,
                    max: 1f,
                    interval: 0.01f
                );
            }
        }

        page.Separator(simple: true);

        page.SectionLabel(() => "ModData/LocationPacks.json");
        foreach (var pack in locationPacks)
        {
            var packKey = pack.Key;
            var packValue = pack.Value;

            page.Int($"debug.json.locationPacks.{packKey}.minSpawnsPerDay",
                getValue: () => packValue.MinimumSpawnsPerDay,
                setValue: v => UpdateLocationPacks(() => packValue.MinimumSpawnsPerDay = v),
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
        _spawnAreas = reader.ReadJsonFile<Dictionary<string, List<SpawnAreaRuleJson>>>(ModDataPaths.SpawnAreas) ?? new Dictionary<string, List<SpawnAreaRuleJson>>();
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

    public void BuildSpawnChanceModifiersPage(GmcmPageBuilder<ModConfig> page)
    {
        page.SectionLabel(() => "Tuning – Egg Spawn Chance");
        page.Float(cfg => cfg.DebugKnobs.SpawningChanceModifiers.LargeMagic, optionId: "debug.tuning.spawnChance.largeMagic", tooltip: true, min: -1f, max: 1f, interval: 0.01f);
        page.Float(cfg => cfg.DebugKnobs.SpawningChanceModifiers.LargeWild, optionId: "debug.tuning.spawnChance.largeWild", tooltip: true, min: -1f, max: 1f, interval: 0.01f);
        page.Float(cfg => cfg.DebugKnobs.SpawningChanceModifiers.Magic, optionId: "debug.tuning.spawnChance.magic", tooltip: true, min: -1f, max: 1f, interval: 0.01f);
    }
}
#endif
