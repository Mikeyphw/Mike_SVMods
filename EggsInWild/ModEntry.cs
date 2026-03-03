using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Mikew.NestCore.Helpers;
using Mikew.NestCore.Helpers.Content;
using Mikew.NestCore.Helpers.Config;
using Mikew.NestCore.Helpers.Logging;
using Mikew.NestCore.Helpers.SaveData;
using Mikew.NestCore.Integrations.Gmcm;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley.GameData.FarmAnimals;
using EggsInWild.Application.Animals;
using EggsInWild.Application.Facade;
using EggsInWild.Application.Progression;
using EggsInWild.Core.Enums;
using EggsInWild.Core.ShopProgression;
using EggsInWild.Infrastructure.Api;
using EggsInWild.Infrastructure.Content;
using EggsInWild.Infrastructure.Content.Editors;
using EggsInWild.Infrastructure.Commands;
using EggsInWild.Infrastructure.Config;
using EggsInWild.Infrastructure.Diagnostics;
using EggsInWild.Infrastructure.Game;
using EggsInWild.Infrastructure.DesertTomb.Monsters.Definitions;
using EggsInWild.Application.DesertTomb.Runtime;
using EggsInWild.Application.DesertTomb.Generation;
using EggsInWild.Application.DesertTomb.Spawning;
using EggsInWild.Infrastructure.DesertTomb.Harmony;
using EggsInWild.Infrastructure.DesertTomb.Loot;
using EggsInWild.Infrastructure.DesertTomb.Loot.Definitions;
using EggsInWild.Infrastructure.DesertTomb.Monsters;
using EggsInWild.DesertTomb.Core.Configuration;
using EggsInWild.Infrastructure.ModData;
using EggsInWild.Infrastructure.ModData.Models;
using EggsInWild.Infrastructure.Assets;

namespace EggsInWild;

public sealed class ModEntry : Mod
{
    private SmapiConfigProvider<ModConfig>? _config;
    private GmcmConfigHost<ModConfig>? _gmcmHost;
    private WildEggsApi? _api;
    private Harmony? _harmony;
    private ModAssetHandler? _assetHandler;
    private AssetPipelineHost<ContentContext>? _contentPipeline;
    private ProgressionService? _contentProgression;
    private ILog? _contentLog;
    private IModDataLoader? _contentDataLoader;

    private DesertTombRuntime? _desertTombRuntime;
    private bool _desertTombHarmonyInstalled;

#if WEGG_DEV
    private DevJsonTuningService? _devJsonTuning;
    private SpawnParametersJson? _devSpawnParametersJson;
    private Dictionary<string, TombMonsterDefinitionJson>? _devMonsterDefinitionsJson;

    private SpawnParametersJson DevSpawnParameters
    {
        get
        {
            if (_devSpawnParametersJson is not null)
                return _devSpawnParametersJson;

            var reader = new SmapiJsonFileReader(Helper);
            _devSpawnParametersJson = reader.ReadJsonFile<SpawnParametersJson>(ModDataPaths.SpawnParameters) ?? new SpawnParametersJson();
            return _devSpawnParametersJson;
        }
    }

    private void MutateDevSpawnParameters(Action<SpawnParametersJson> mutate)
    {
        mutate(DevSpawnParameters);
        Helper.Data.WriteJsonFile(ModDataPaths.SpawnParameters, DevSpawnParameters);
    }


    private Dictionary<string, TombMonsterDefinitionJson> DevMonsterDefinitions
    {
        get
        {
            if (_devMonsterDefinitionsJson is not null)
                return _devMonsterDefinitionsJson;

            var reader = new SmapiJsonFileReader(Helper);
            _devMonsterDefinitionsJson = reader.ReadJsonFile<Dictionary<string, TombMonsterDefinitionJson>>(DesertTombDefinitionPaths.MonsterDefinitions)
                ?? new Dictionary<string, TombMonsterDefinitionJson>(StringComparer.OrdinalIgnoreCase);

            return _devMonsterDefinitionsJson;
        }
    }

    private void MutateDevMonsterDefinitions(Action mutate)
    {
        mutate();
        Helper.Data.WriteJsonFile(DesertTombDefinitionPaths.MonsterDefinitions, DevMonsterDefinitions);
    }

    private static string NormalizeDevJsonKey(string raw)
        => string.IsNullOrWhiteSpace(raw) ? "empty" : new string(raw.Select(ch => char.IsLetterOrDigit(ch) ? ch : '_').ToArray());
#endif


    public override void Entry(IModHelper helper)
    {
        _config = new SmapiConfigProvider<ModConfig>(helper, Monitor);
        _config.ConfigUpdated += OnConfigUpdated;

        DebugKnobs.Initialize(() => _config.Get());

        EnsureNoLocalNestCoreDuplicates(helper);
        RegisterConsoleCommands(helper);

        helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        helper.Events.GameLoop.DayStarted += OnDayStarted;
        helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        InitializeModAssetHandler();
        InitializeContentPipeline();

        InstallHarmonyBootstrap();
        TryInstallDesertTombHarmonyPatches(reason: "startup");
    }

    public override object? GetApi()
    {
        if (_config is null)
            return null;

        _api ??= new WildEggsApi(() => _config.Get());
        SyncApiBirthTextMode();
        return _api;
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        if (_config is null)
            return;

        _gmcmHost = GmcmConfigHost<ModConfig>.TryCreate(
            helper: Helper,
            manifest: ModManifest,
            configProvider: _config
        );

#if WEGG_DEV
        _devJsonTuning = new DevJsonTuningService(Helper, new SmapiLog(Monitor));
#endif

        RebuildGmcmLayout(force: true);
    }

    private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        _contentProgression?.OnDayStarted();
        SyncApiBirthTextMode();
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        _contentProgression?.OnSaveLoaded();
    }
    private void RebuildGmcmLayout(bool force)
    {
        if (_gmcmHost is null)
            return;


        var i18n = new I18TranslationHelper(Helper);
        _gmcmHost.RebuildLayoutSaveAware(define: layout =>
        {
            layout.Root(root =>
            {
                root.Bool(cfg => cfg.ProgressionMode, optionId: "general.progressionMode", tooltip: true);
                root.Separator(simple: true);

                root.Link("desertTomb", tooltip: true);
                root.Link("hatching", tooltip: true);
                root.Link("items", tooltip: true);
                root.Link("prices", tooltip: true);
                root.Link("animalNotes", tooltip: true);
                root.Link("animalProgression", tooltip: true);
                root.Link("wildAnimals", tooltip: true);
#if WEGG_DEV
                root.Link("debug", tooltip: true);
#endif
            });

            layout.Page("desertTomb", page =>
            {
                page.Bool(cfg => cfg.DesertTombModuleEnabled, optionId: "desertTomb.enabled", tooltip: true);
                page.Bool(cfg => cfg.KeepMineLevel, optionId: "desertTomb.keepMineLevel", tooltip: true);
                page.Enum(cfg => cfg.TombChance, optionId: "desertTomb.tombChance", tooltip: true);
                page.Enum(cfg => cfg.TombDifficulty, optionId: "desertTomb.tombDifficulty", tooltip: true);
            });

            layout.Page("hatching", page =>
            {
                page.Bool(cfg => cfg.HatchDinoEggs, optionId: "hatching.hatchDinoEggs", tooltip: true);
                page.Bool(cfg => cfg.DinoEggsExclusiveHatch, optionId: "hatching.dinoExclusiveHatch", tooltip: true);
                page.Text(cfg => cfg.BirthText, optionId: "general.birthText", tooltip: true);
            });

            layout.Page("items", page =>
            {
                page.Bool(cfg => cfg.AlternatePotionSprite, optionId: "items.alternatePotionSprite", tooltip: true);
                page.Bool(cfg => cfg.NeedsFern, optionId: "items.incubatorNeedsFern", tooltip: true);
                page.Bool(cfg => cfg.FruitInsteadOfSeed, optionId: "items.fruitInsteadOfSeed", tooltip: true);
            });

            layout.Page("prices", page =>
            {
                page.Text(cfg => cfg.DinoPrice, optionId: "prices.dino", tooltip: true);
                page.Text(cfg => cfg.VoidChickenPrice, optionId: "prices.voidChicken", tooltip: true);
                page.Text(cfg => cfg.OstrichPrice, optionId: "prices.ostrich", tooltip: true);
            });

            layout.Page("animalNotes", page =>
            {
                page.Int(cfg => cfg.AnimalNotesWarnAfterDays, optionId: "animalNotes.warnAfterDays", tooltip: true, min: 1, max: 30, interval: 1);
                page.Int(cfg => cfg.AnimalNotesPlacementRadius, optionId: "animalNotes.placementRadius", tooltip: true, min: 1, max: 30, interval: 1);
                page.Bool(cfg => cfg.AnimalNotesQuestMailHostOnly, optionId: "animalNotes.hostOnlyQuestMail", tooltip: true);
                page.Bool(cfg => cfg.AnimalNotesAllowClientDiscoveries, optionId: "animalNotes.allowClientDiscoveries", tooltip: true);
                page.ParagraphDynamic(
                    () => Translate(i18n, "config.paragraph.animalNotes.progressionOverrides.text", "Per-animal progression overrides are managed on the Animal Progression page."),
                    id: "animalNotes_progression_hint");
                page.Link("animalProgression", tooltip: true);
            });

            layout.Page("animalProgression", page =>
            {
                page.ParagraphDynamic(
                    () => Translate(i18n, "config.paragraph.animalProgression.description.text", "Configure per-animal progression gating."),
                    id: "animalProgression_intro");
                BuildAnimalProgressionPage(page, i18n);
            });
// ============================
// Wild Animals (GMCM editable)
// ============================

Dictionary<string, EggsInWild.Infrastructure.ModData.Models.WildAnimalJson> wildAnimalsForGmcm;
try
{
    var reader = new EggsInWild.Infrastructure.ModData.SmapiJsonFileReader(Helper);
    wildAnimalsForGmcm =
        reader.ReadJsonFile<Dictionary<string, EggsInWild.Infrastructure.ModData.Models.WildAnimalJson>>(EggsInWild.Infrastructure.ModData.ModDataPaths.WildAnimalsList)
        ?? new Dictionary<string, EggsInWild.Infrastructure.ModData.Models.WildAnimalJson>(StringComparer.OrdinalIgnoreCase);
}
catch (Exception ex)
{
    var smapiLog = new SmapiLog(Monitor);
    smapiLog.LogScoped(ScopedLogCatalog.ModWeCategory.Content, $"Failed to load {EggsInWild.Infrastructure.ModData.ModDataPaths.WildAnimalsList} for GMCM: {ex}", LogLevel.Error);
    wildAnimalsForGmcm = new Dictionary<string, EggsInWild.Infrastructure.ModData.Models.WildAnimalJson>(StringComparer.OrdinalIgnoreCase);
}

var wildAnimalKeysForGmcm = wildAnimalsForGmcm.Keys
    .OrderBy(k => k, StringComparer.OrdinalIgnoreCase)
    .ToList();

var locationTypeKeysForGmcm = wildAnimalsForGmcm.Values
    .SelectMany(v => v.LocationTypes?.Keys ?? Enumerable.Empty<string>())
    .Where(k => !string.IsNullOrWhiteSpace(k))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .OrderBy(k => string.Equals(k, "Farm", StringComparison.OrdinalIgnoreCase) ? "" : k, StringComparer.OrdinalIgnoreCase)
    .ToList();

layout.Page("wildAnimals", page =>
{
    page.ParagraphDynamic(
        () => Translate(i18n, "config.paragraph.wildAnimals.description.text", "Enable/disable wild animals (affects egg assignment and Animal Notes progression)."),
        id: "wildAnimals_description");

    page.ParagraphDynamic(
        () =>
        {
            var cfg = _config!.Get();
            var enabled = 0;
            foreach (var key in wildAnimalKeysForGmcm)
            {
                if (!wildAnimalsForGmcm.TryGetValue(key, out var def) || def is null)
                    continue;

                var defaultEnabled = def.Enabled;
                var effective = GetWildAnimalEffectiveEnabled(cfg, key, defaultEnabled);
                if (effective)
                    enabled++;
            }

            return Translate(i18n, "config.paragraph.wildAnimals.summary.text", $"Enabled: {enabled}/{wildAnimalKeysForGmcm.Count}", enabled, wildAnimalKeysForGmcm.Count);
        },
        id: "wildAnimals_summary");

    if (wildAnimalKeysForGmcm.Count == 0)
    {
        page.ParagraphDynamic(
            () => Translate(i18n, "config.paragraph.wildAnimals.empty.text", "No wild animals were found in the WildAnimalsList."),
            id: "wildAnimals_empty");
        return;
    }

    page.Separator(simple: true);

    foreach (var key in wildAnimalKeysForGmcm)
    {
        var pageId = BuildWildAnimalPageId(key);
        page.LinkLabel(
            pageId: pageId,
            text: () =>
            {
                var cfg = _config!.Get();
                var enabled = wildAnimalsForGmcm.TryGetValue(key, out var def) && def is not null
                    ? GetWildAnimalEffectiveEnabled(cfg, key, def.Enabled)
                    : true;

                return enabled ? key : $"{key} (disabled)";
            },
            tooltip: () => Translate(i18n, "config.wildAnimals.link.tooltip", "Edit enabled override for '{0}'.", key, key));
    }
});

foreach (var key in wildAnimalKeysForGmcm)
{
    var pageId = BuildWildAnimalPageId(key);

    layout.Page(
        pageId: pageId,
        title: () => key,
        build: page =>
        {
            var definition = wildAnimalsForGmcm.TryGetValue(key, out var def) ? def : null;

            var defaultEnabled = definition?.Enabled ?? true;
            var defaultLargeEgg = definition?.LargeEgg ?? false;
            var defaultPrice = definition?.Price ?? 0;
            var defaultIsMagic = definition?.IsMagic ?? false;

            var modAvailability = new EggsInWild.Infrastructure.Mods.SmapiModAvailability(Helper.ModRegistry);

            static bool IsEmptyOverride(WildAnimalOverrideConfig? ov)
                => ov is null
                    || (ov.Enabled is null
                        && ov.LargeEgg is null
                        && ov.Price is null
                        && ov.IsMagic is null
                        && (ov.LocationTypes is null || ov.LocationTypes.Count == 0)
                        && (ov.ExtraModAnimalKeys is null || ov.ExtraModAnimalKeys.Count == 0));

            WildAnimalOverrideConfig? GetOverride(ModConfig cfg)
            {
                if (cfg.WildAnimalOverrides is null)
                    return null;

                cfg.WildAnimalOverrides.TryGetValue(key, out var ov);
                return ov;
            }

            page.ParagraphDynamic(
                () =>
                {
                    var cfg = _config!.Get();
                    var source = GetWildAnimalEnabledSource(cfg, key, defaultEnabled);
                    var effective = GetWildAnimalEffectiveEnabled(cfg, key, defaultEnabled);

                    var state = effective
                        ? Translate(i18n, "config.wildAnimals.state.enabled", "Enabled")
                        : Translate(i18n, "config.wildAnimals.state.disabled", "Disabled");

                    return Translate(i18n, "config.paragraph.wildAnimals.status.text", $"Effective: {state} (source: {source})", state, source);
                },
                id: $"wildAnimals_status_{Mikew.NestCore.Integrations.Gmcm.GmcmFieldIds.Normalize(key)}");

            page.ParagraphDynamic(
                () =>
                {
                    var cfg = _config!.Get();
                    var ov = GetOverride(cfg);

                    var largeEggOverride = ov?.LargeEgg;
                    var priceOverride = ov?.Price;
                    var isMagicOverride = ov?.IsMagic;

                    var effectiveLargeEgg = largeEggOverride ?? defaultLargeEgg;
                    var effectivePrice = priceOverride ?? defaultPrice;
                    var effectiveIsMagic = isMagicOverride ?? defaultIsMagic;

                    var largeEggSource = largeEggOverride is null ? "default" : "override";
                    var priceSource = priceOverride is null ? "default" : "override";
                    var isMagicSource = isMagicOverride is null ? "default" : "override";

                    var yes = Translate(i18n, "config.wildAnimals.bool.yes", "Yes");
                    var no = Translate(i18n, "config.wildAnimals.bool.no", "No");

                    string BoolToYesNo(bool v) => v ? yes : no;

                    return Translate(
                        i18n,
                        "config.paragraph.wildAnimals.scalars.text",
                        $"Effective scalars: LargeEgg={BoolToYesNo(effectiveLargeEgg)} (source: {largeEggSource}); Price={effectivePrice} (source: {priceSource}); IsMagic={BoolToYesNo(effectiveIsMagic)} (source: {isMagicSource}).",
                        BoolToYesNo(effectiveLargeEgg),
                        largeEggSource,
                        effectivePrice,
                        priceSource,
                        BoolToYesNo(effectiveIsMagic),
                        isMagicSource);
                },
                id: $"wildAnimals_scalars_{Mikew.NestCore.Integrations.Gmcm.GmcmFieldIds.Normalize(key)}");

            page.ParagraphDynamic(
                () =>
                {
                    var cfg = _config!.Get();
                    var ov = GetOverride(cfg);

                    var yes = Translate(i18n, "config.wildAnimals.bool.yes", "Yes");
                    var no = Translate(i18n, "config.wildAnimals.bool.no", "No");
                    var unset = Translate(i18n, "config.wildAnimals.value.unset", "(unset)");

                    string BoolToYesNo(bool v) => v ? yes : no;
                    string FormatOverrideBool(bool? v) => v is null ? unset : BoolToYesNo(v.Value);
                    string FormatOverrideInt(int? v) => v is null ? unset : v.Value.ToString();

                    var enabledText = FormatOverrideBool(ov?.Enabled);
                    var largeEggText = FormatOverrideBool(ov?.LargeEgg);
                    var priceText = FormatOverrideInt(ov?.Price);
                    var isMagicText = FormatOverrideBool(ov?.IsMagic);

                    return Translate(
                        i18n,
                        "config.paragraph.wildAnimals.overrides.text",
                        $"Overrides: Enabled={enabledText}; LargeEgg={largeEggText}; Price={priceText}; IsMagic={isMagicText}.",
                        enabledText,
                        largeEggText,
                        priceText,
                        isMagicText);
                },
                id: $"wildAnimals_overrides_{Mikew.NestCore.Integrations.Gmcm.GmcmFieldIds.Normalize(key)}");


            page.Separator(simple: true);

            page.BoolLabel(
                elementId: $"wildAnimals.{key}.enabled",
                name: () => Translate(i18n, "config.wildAnimals.enabled.name", "Enabled"),
                tooltip: () =>
                {
                    var defState = defaultEnabled
                        ? Translate(i18n, "config.wildAnimals.state.enabled", "Enabled")
                        : Translate(i18n, "config.wildAnimals.state.disabled", "Disabled");

                    return Translate(
                        i18n,
                        "config.wildAnimals.enabled.tooltip",
                        $"Override enabled state for '{key}'. Default: {defState}.",
                        key,
                        defState);
                },
                getValue: () =>
                {
                    var cfg = _config!.Get();
                    return GetWildAnimalEffectiveEnabled(cfg, key, defaultEnabled);
                },
                setValue: value =>
                {
                    var cfg = _config!.Get();
                    cfg.WildAnimalOverrides ??= new Dictionary<string, WildAnimalOverrideConfig>(StringComparer.OrdinalIgnoreCase);

                    if (!cfg.WildAnimalOverrides.TryGetValue(key, out var ov) || ov is null)
                        ov = new WildAnimalOverrideConfig();

                    ov.Enabled = value == defaultEnabled ? null : value;

                    if (IsEmptyOverride(ov))
                        cfg.WildAnimalOverrides.Remove(key);
                    else
                        cfg.WildAnimalOverrides[key] = ov;
                });

            page.BoolLabel(
                elementId: $"wildAnimals.{key}.largeEgg",
                name: () => Translate(i18n, "config.wildAnimals.largeEgg.name", "Large egg"),
                tooltip: () =>
                {
                    var yes = Translate(i18n, "config.wildAnimals.bool.yes", "Yes");
                    var no = Translate(i18n, "config.wildAnimals.bool.no", "No");
                    var defVal = defaultLargeEgg ? yes : no;

                    return Translate(
                        i18n,
                        "config.wildAnimals.largeEgg.tooltip",
                        $"Override LargeEgg flag for '{key}'. Default: {defVal}.",
                        key,
                        defVal);
                },
                getValue: () =>
                {
                    var cfg = _config!.Get();
                    var ov = GetOverride(cfg);
                    return ov?.LargeEgg ?? defaultLargeEgg;
                },
                setValue: value =>
                {
                    var cfg = _config!.Get();
                    cfg.WildAnimalOverrides ??= new Dictionary<string, WildAnimalOverrideConfig>(StringComparer.OrdinalIgnoreCase);

                    if (!cfg.WildAnimalOverrides.TryGetValue(key, out var ov) || ov is null)
                        ov = new WildAnimalOverrideConfig();

                    ov.LargeEgg = value == defaultLargeEgg ? null : value;

                    if (IsEmptyOverride(ov))
                        cfg.WildAnimalOverrides.Remove(key);
                    else
                        cfg.WildAnimalOverrides[key] = ov;
                });

            page.IntLabel(
                elementId: $"wildAnimals.{key}.price",
                name: () => Translate(i18n, "config.wildAnimals.price.name", "Price"),
                tooltip: () => Translate(
                    i18n,
                    "config.wildAnimals.price.tooltip",
                    $"Override price for '{key}'. Default: {defaultPrice}.",
                    key,
                    defaultPrice),
                getValue: () =>
                {
                    var cfg = _config!.Get();
                    var ov = GetOverride(cfg);
                    return ov?.Price ?? defaultPrice;
                },
                setValue: value =>
                {
                    var cfg = _config!.Get();
                    cfg.WildAnimalOverrides ??= new Dictionary<string, WildAnimalOverrideConfig>(StringComparer.OrdinalIgnoreCase);

                    if (!cfg.WildAnimalOverrides.TryGetValue(key, out var ov) || ov is null)
                        ov = new WildAnimalOverrideConfig();

                    ov.Price = value == defaultPrice ? null : value;

                    if (IsEmptyOverride(ov))
                        cfg.WildAnimalOverrides.Remove(key);
                    else
                        cfg.WildAnimalOverrides[key] = ov;
                },
                min: 0,
                max: 20000,
                interval: 25);

            page.BoolLabel(
                elementId: $"wildAnimals.{key}.isMagic",
                name: () => Translate(i18n, "config.wildAnimals.isMagic.name", "Magic"),
                tooltip: () =>
                {
                    var yes = Translate(i18n, "config.wildAnimals.bool.yes", "Yes");
                    var no = Translate(i18n, "config.wildAnimals.bool.no", "No");
                    var defVal = defaultIsMagic ? yes : no;

                    return Translate(
                        i18n,
                        "config.wildAnimals.isMagic.tooltip",
                        $"Override IsMagic flag for '{key}'. Default: {defVal}.",
                        key,
                        defVal);
                },
                getValue: () =>
                {
                    var cfg = _config!.Get();
                    var ov = GetOverride(cfg);
                    return ov?.IsMagic ?? defaultIsMagic;
                },
                setValue: value =>
                {
                    var cfg = _config!.Get();
                    cfg.WildAnimalOverrides ??= new Dictionary<string, WildAnimalOverrideConfig>(StringComparer.OrdinalIgnoreCase);

                    if (!cfg.WildAnimalOverrides.TryGetValue(key, out var ov) || ov is null)
                        ov = new WildAnimalOverrideConfig();

                    ov.IsMagic = value == defaultIsMagic ? null : value;

                    if (IsEmptyOverride(ov))
                        cfg.WildAnimalOverrides.Remove(key);
                    else
                        cfg.WildAnimalOverrides[key] = ov;
                });


            page.Separator(simple: true);
            page.SectionLabel(() => Translate(i18n, "config.section.wildAnimals.locationTypes.label", "Location types"));

            page.ParagraphDynamic(
                () => Translate(
                    i18n,
                    "config.paragraph.wildAnimals.locationTypes.description.text",
                    "Override eligibility per location type. Use Default to inherit from WildAnimalsList."),
                id: $"wildAnimals_locationTypes_{Mikew.NestCore.Integrations.Gmcm.GmcmFieldIds.Normalize(key)}");

            var allowedValues = new[] { "default", "allow", "deny" };

            foreach (var locationTypeKey in locationTypeKeysForGmcm)
            {
                var baseAllowed = definition?.LocationTypes is not null
                    && definition.LocationTypes.TryGetValue(locationTypeKey, out var baseVal)
                    && baseVal;

                var elementId = $"wildAnimals.{key}.locationTypes.{locationTypeKey}";

                page.TextLabel(
                    elementId: elementId,
                    name: () =>
                    {
                        var typeName = Translate(
                            i18n,
                            $"config.wildAnimals.locationTypes.type.{locationTypeKey}.name",
                            locationTypeKey);

                        return typeName;
                    },
                    tooltip: () =>
                    {
                        var defState = baseAllowed
                            ? Translate(i18n, "config.wildAnimals.state.enabled", "Enabled")
                            : Translate(i18n, "config.wildAnimals.state.disabled", "Disabled");

                        var typeName = Translate(
                            i18n,
                            $"config.wildAnimals.locationTypes.type.{locationTypeKey}.name",
                            locationTypeKey);

                        return Translate(
                            i18n,
                            "config.wildAnimals.locationTypes.type.tooltip",
                            $"Override eligibility for location type '{typeName}'. Default: {defState}.",
                            typeName,
                            defState);
                    },
                    getValue: () =>
                    {
                        var cfg = _config!.Get();
                        var ov = GetOverride(cfg);

                        if (ov?.LocationTypes is not null && ov.LocationTypes.TryGetValue(locationTypeKey, out var o))
                            return o ? "allow" : "deny";

                        return "default";
                    },
                    setValue: value =>
                    {
                        var cfg = _config!.Get();
                        cfg.WildAnimalOverrides ??= new Dictionary<string, WildAnimalOverrideConfig>(StringComparer.OrdinalIgnoreCase);

                        if (!cfg.WildAnimalOverrides.TryGetValue(key, out var ov) || ov is null)
                            ov = new WildAnimalOverrideConfig();

                        ov.LocationTypes ??= new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

                        if (string.Equals(value, "default", StringComparison.OrdinalIgnoreCase)
                            || (string.Equals(value, "allow", StringComparison.OrdinalIgnoreCase) && baseAllowed)
                            || (string.Equals(value, "deny", StringComparison.OrdinalIgnoreCase) && !baseAllowed))
                        {
                            ov.LocationTypes.Remove(locationTypeKey);
                        }
                        else if (string.Equals(value, "allow", StringComparison.OrdinalIgnoreCase))
                        {
                            ov.LocationTypes[locationTypeKey] = true;
                        }
                        else if (string.Equals(value, "deny", StringComparison.OrdinalIgnoreCase))
                        {
                            ov.LocationTypes[locationTypeKey] = false;
                        }

                        if (ov.LocationTypes.Count == 0)
                            ov.LocationTypes = null;

                        if (IsEmptyOverride(ov))
                            cfg.WildAnimalOverrides.Remove(key);
                        else
                            cfg.WildAnimalOverrides[key] = ov;
                    },
                    allowedValues: allowedValues,
                    formatValue: v =>
                    {
                        if (string.Equals(v, "allow", StringComparison.OrdinalIgnoreCase))
                            return Translate(i18n, "config.wildAnimals.locationTypes.choice.allow", "Allow");

                        if (string.Equals(v, "deny", StringComparison.OrdinalIgnoreCase))
                            return Translate(i18n, "config.wildAnimals.locationTypes.choice.deny", "Deny");

                        var defState = baseAllowed
                            ? Translate(i18n, "config.wildAnimals.locationTypes.choice.default.enabled", "Default (Enabled)")
                            : Translate(i18n, "config.wildAnimals.locationTypes.choice.default.disabled", "Default (Disabled)");

                        return defState;
                    });
            }



            page.Separator(simple: true);
            page.SectionLabel(() => Translate(i18n, "config.section.wildAnimals.extraModAnimalKeys.label", "Farm animal keys"));

            page.ParagraphDynamic(
                () => Translate(
                    i18n,
                    "config.paragraph.wildAnimals.extraModAnimalKeys.description.text",
                    "Edit ExtraModAnimalKeys per mod group. Enter a comma-separated list of FarmAnimalKeys. Blank means default (inherit from WildAnimalsList)."),
                id: $"wildAnimals_extraModAnimalKeys_{Mikew.NestCore.Integrations.Gmcm.GmcmFieldIds.Normalize(key)}");

            var extraModIdsForGmcm = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Vanilla",
            };

            if (definition?.ExtraModAnimalKeys is not null)
            {
                foreach (var modId in definition.ExtraModAnimalKeys.Keys)
                    extraModIdsForGmcm.Add(modId);
            }

            {
                var cfg = _config!.Get();
                var ov = GetOverride(cfg);
                if (ov?.ExtraModAnimalKeys is not null)
                {
                    foreach (var modId in ov.ExtraModAnimalKeys.Keys)
                        extraModIdsForGmcm.Add(modId);
                }
            }

            var extraModIdsOrdered = extraModIdsForGmcm
                .OrderBy(mid => mid == "Vanilla" ? "" : mid, StringComparer.OrdinalIgnoreCase)
                .ToList();

            
            page.TextLabel(
                elementId: $"wildAnimals.{key}.extraModAnimalKeys.customUpsert",
                name: () => Translate(i18n, "config.wildAnimals.extraModAnimalKeys.customUpsert.name", "Add/update custom modId block"),
                tooltip: () => Translate(
                    i18n,
                    "config.wildAnimals.extraModAnimalKeys.customUpsert.tooltip",
                    "Create or update a custom mod-group block not present in WildAnimalsList. Format: modId | csvKeys (example: Author.ModName | Pig, Cow). Close and reopen this page to refresh the list."),
                getValue: () => "",
                setValue: rawValue =>
                {
                    var input = (rawValue ?? string.Empty).Trim();
                    if (input.Length == 0)
                        return;

                    var sepIndex = input.IndexOf('|');
                    if (sepIndex < 0) sepIndex = input.IndexOf(':');
                    if (sepIndex < 0) sepIndex = input.IndexOf('=');

                    if (sepIndex <= 0 || sepIndex >= input.Length - 1)
                        return;

                    var modIdRaw = input[..sepIndex].Trim();
                    var keysRaw = input[(sepIndex + 1)..].Trim();

                    if (modIdRaw.Length == 0 || string.Equals(modIdRaw, "Vanilla", StringComparison.OrdinalIgnoreCase))
                        return;

                    var parsed = ParseCsvKeyList(keysRaw);
                    if (parsed.Count == 0)
                        return;

                    var modId = modIdRaw;
                    foreach (var existing in extraModIdsForGmcm)
                    {
                        if (string.Equals(existing, modIdRaw, StringComparison.OrdinalIgnoreCase))
                        {
                            modId = existing;
                            break;
                        }
                    }

                    var baseKeys = definition?.ExtraModAnimalKeys is not null
                        && definition.ExtraModAnimalKeys.TryGetValue(modId, out var baseBlock)
                        ? baseBlock.GetEffectiveFarmAnimalKeys()
                        : Array.Empty<string>();

                    var cfg = _config!.Get();
                    cfg.WildAnimalOverrides ??= new Dictionary<string, WildAnimalOverrideConfig>(StringComparer.OrdinalIgnoreCase);

                    if (!cfg.WildAnimalOverrides.TryGetValue(key, out var ov) || ov is null)
                        ov = new WildAnimalOverrideConfig();

                    ov.ExtraModAnimalKeys ??= new Dictionary<string, ExtraModAnimalKeysOverrideConfig>(StringComparer.OrdinalIgnoreCase);

                    if (SequenceEqualIgnoreCase(parsed, baseKeys))
                    {
                        ov.ExtraModAnimalKeys.Remove(modId);
                    }
                    else
                    {
                        ov.ExtraModAnimalKeys[modId] = new ExtraModAnimalKeysOverrideConfig
                        {
                            FarmAnimalKeys = parsed.ToList(),
                        };
                    }

                    if (ov.ExtraModAnimalKeys.Count == 0)
                        ov.ExtraModAnimalKeys = null;

                    if (IsEmptyOverride(ov))
                        cfg.WildAnimalOverrides.Remove(key);
                    else
                        cfg.WildAnimalOverrides[key] = ov;
                });

            page.TextLabel(
                elementId: $"wildAnimals.{key}.extraModAnimalKeys.customRemove",
                name: () => Translate(i18n, "config.wildAnimals.extraModAnimalKeys.customRemove.name", "Remove modId override block"),
                tooltip: () => Translate(
                    i18n,
                    "config.wildAnimals.extraModAnimalKeys.customRemove.tooltip",
                    "Remove an ExtraModAnimalKeys override block by modId. This resets to default for base blocks, and deletes custom blocks. Enter: modId. Close and reopen this page to refresh the list."),
                getValue: () => "",
                setValue: rawValue =>
                {
                    var modIdRaw = (rawValue ?? string.Empty).Trim();
                    if (modIdRaw.Length == 0 || string.Equals(modIdRaw, "Vanilla", StringComparison.OrdinalIgnoreCase))
                        return;

                    var modId = modIdRaw;
                    foreach (var existing in extraModIdsForGmcm)
                    {
                        if (string.Equals(existing, modIdRaw, StringComparison.OrdinalIgnoreCase))
                        {
                            modId = existing;
                            break;
                        }
                    }

                    var cfg = _config!.Get();
                    var ov = GetOverride(cfg);
                    if (ov?.ExtraModAnimalKeys is null)
                        return;

                    ov.ExtraModAnimalKeys.Remove(modId);

                    if (ov.ExtraModAnimalKeys.Count == 0)
                        ov.ExtraModAnimalKeys = null;

                    if (IsEmptyOverride(ov))
                        cfg.WildAnimalOverrides.Remove(key);
                    else
                        cfg.WildAnimalOverrides[key] = ov;
                });

            page.ParagraphDynamic(
                () => Translate(
                    i18n,
                    "config.wildAnimals.extraModAnimalKeys.customRefreshNote.text",
                    "After adding/removing blocks, close and reopen this page to refresh the list."),
                id: $"wildAnimals_extraModAnimalKeys_customRefresh_{Mikew.NestCore.Integrations.Gmcm.GmcmFieldIds.Normalize(key)}");

foreach (var modId in extraModIdsOrdered)
            {
                var elementId = $"wildAnimals.{key}.extraModAnimalKeys.{Mikew.NestCore.Integrations.Gmcm.GmcmFieldIds.Normalize(modId)}";

                page.TextLabel(
                    elementId: elementId,
                    name: () => modId == "Vanilla"
                        ? Translate(i18n, "config.wildAnimals.extraModAnimalKeys.modId.vanilla.name", "Vanilla")
                        : modId,
                    tooltip: () =>
                    {
                        var loaded = modAvailability.IsLoaded(modId)
                            ? Translate(i18n, "config.wildAnimals.state.loaded", "Loaded")
                            : Translate(i18n, "config.wildAnimals.state.notLoaded", "Not loaded");

                        return Translate(
                            i18n,
                            "config.wildAnimals.extraModAnimalKeys.modId.tooltip",
                            "FarmAnimalKeys for mod-group '{0}'. Mod is {1}.",
                            modId,
                            loaded);
                    },
                    getValue: () =>
                    {
                        var cfg = _config!.Get();
                        var ov = GetOverride(cfg);

                        if (ov?.ExtraModAnimalKeys is not null
                            && ov.ExtraModAnimalKeys.TryGetValue(modId, out var blockOv)
                            && blockOv?.FarmAnimalKeys is { Count: > 0 })
                        {
                            return string.Join(", ", blockOv.FarmAnimalKeys);
                        }

                        var baseKeys = definition?.ExtraModAnimalKeys is not null
                            && definition.ExtraModAnimalKeys.TryGetValue(modId, out var baseBlock)
                            ? baseBlock.GetEffectiveFarmAnimalKeys()
                            : Array.Empty<string>();

                        return baseKeys.Count == 0 ? "" : string.Join(", ", baseKeys);
                    },
                    setValue: rawValue =>
                    {
                        var parsed = ParseCsvKeyList(rawValue);

                        var baseKeys = definition?.ExtraModAnimalKeys is not null
                            && definition.ExtraModAnimalKeys.TryGetValue(modId, out var baseBlock)
                            ? baseBlock.GetEffectiveFarmAnimalKeys()
                            : Array.Empty<string>();

                        var cfg = _config!.Get();
                        cfg.WildAnimalOverrides ??= new Dictionary<string, WildAnimalOverrideConfig>(StringComparer.OrdinalIgnoreCase);

                        if (!cfg.WildAnimalOverrides.TryGetValue(key, out var ov) || ov is null)
                            ov = new WildAnimalOverrideConfig();

                        ov.ExtraModAnimalKeys ??= new Dictionary<string, ExtraModAnimalKeysOverrideConfig>(StringComparer.OrdinalIgnoreCase);

                        if (parsed.Count == 0 || SequenceEqualIgnoreCase(parsed, baseKeys))
                        {
                            ov.ExtraModAnimalKeys.Remove(modId);
                        }
                        else
                        {
                            ov.ExtraModAnimalKeys[modId] = new ExtraModAnimalKeysOverrideConfig
                            {
                                FarmAnimalKeys = parsed.ToList(),
                            };
                        }

                        if (ov.ExtraModAnimalKeys.Count == 0)
                            ov.ExtraModAnimalKeys = null;

                        if (IsEmptyOverride(ov))
                            cfg.WildAnimalOverrides.Remove(key);
                        else
                            cfg.WildAnimalOverrides[key] = ov;
                    });


                page.BoolLabel(
                    elementId: $"{elementId}.reset",
                    name: () => Translate(i18n, "config.wildAnimals.extraModAnimalKeys.resetBlock.name", "Reset block to default"),
                    tooltip: () => Translate(
                        i18n,
                        "config.wildAnimals.extraModAnimalKeys.resetBlock.tooltip",
                        "Remove the override for mod-group '{0}'. For custom blocks this deletes the block. Close and reopen this page to refresh the list.",
                        modId),
                    getValue: () => false,
                    setValue: reset =>
                    {
                        if (!reset)
                            return;

                        var cfg = _config!.Get();
                        var ov = GetOverride(cfg);
                        if (ov?.ExtraModAnimalKeys is null)
                            return;

                        ov.ExtraModAnimalKeys.Remove(modId);

                        if (ov.ExtraModAnimalKeys.Count == 0)
                            ov.ExtraModAnimalKeys = null;

                        if (IsEmptyOverride(ov))
                            cfg.WildAnimalOverrides.Remove(key);
                        else
                            cfg.WildAnimalOverrides[key] = ov;
                    });
            }

            page.Separator(simple: true);
            page.SectionLabel(() => Translate(i18n, "config.section.wildAnimals.diagnostics.label", "Diagnostics"));

            page.ParagraphDynamic(
                () =>
                {
                    if (definition?.ExtraModAnimalKeys is null || definition.ExtraModAnimalKeys.Count == 0)
                        return Translate(i18n, "config.paragraph.wildAnimals.diagnostics.noSources.text", "ExtraModAnimalKeys: (none)");

                    var ordered = definition.ExtraModAnimalKeys
                        .OrderBy(kvp => kvp.Key == "Vanilla" ? "" : kvp.Key, StringComparer.OrdinalIgnoreCase);

                    var lines = new List<string>();
                    foreach (var (modId, keys) in ordered)
                    {
                        var loaded = modAvailability.IsLoaded(modId);
                        var raw = keys?.FarmAnimalKeys ?? new List<string>();
                        var effective = keys?.GetEffectiveFarmAnimalKeys() ?? Array.Empty<string>();

                        var rawJoined = raw.Count == 0 ? "" : string.Join(", ", raw);
                        var effJoined = effective.Count == 0 ? "" : string.Join(", ", effective);

                        var legacyNote = raw.Count == 0 && effective.Count > 0
                            ? Translate(i18n, "config.wildAnimals.diagnostics.note.legacy", " (legacy fields)")
                            : "";

                        lines.Add($"{modId}: loaded={loaded}; FarmAnimalKeys=[{rawJoined}]{legacyNote}; effective=[{effJoined}]");
                    }

                    var joined = string.Join(Environment.NewLine, lines);
                    return Translate(i18n, "config.paragraph.wildAnimals.diagnostics.sources.text", "ExtraModAnimalKeys sources:\n{0}", joined);
                },
                id: $"wildAnimals_diag_sources_{Mikew.NestCore.Integrations.Gmcm.GmcmFieldIds.Normalize(key)}");

            page.ParagraphDynamic(
                () =>
                {
                    if (definition?.ExtraModAnimalKeys is null || definition.ExtraModAnimalKeys.Count == 0)
                        return Translate(i18n, "config.paragraph.wildAnimals.diagnostics.resolvedIds.empty.text", "Resolved FarmAnimalIds: (none)");

                    var ids = new List<string>();
                    var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    foreach (var (modId, keys) in definition.ExtraModAnimalKeys
                        .OrderBy(kvp => kvp.Key == "Vanilla" ? "" : kvp.Key, StringComparer.OrdinalIgnoreCase))
                    {
                        if (!modAvailability.IsLoaded(modId))
                            continue;

                        foreach (var id in keys.GetEffectiveFarmAnimalKeys())
                        {
                            if (seen.Add(id))
                                ids.Add(id);
                        }
                    }

                    var joined = ids.Count == 0
                        ? Translate(i18n, "config.wildAnimals.value.none", "(none)")
                        : string.Join(", ", ids);

                    return Translate(i18n, "config.paragraph.wildAnimals.diagnostics.resolvedIds.text", "Resolved FarmAnimalIds (loaded mods): {0}", joined);
                },
                id: $"wildAnimals_diag_resolved_{Mikew.NestCore.Integrations.Gmcm.GmcmFieldIds.Normalize(key)}");

            page.BoolLabel(
                elementId: $"wildAnimals.{key}.reset",
                name: () => Translate(i18n, "config.wildAnimals.reset.name", "Reset override"),
                tooltip: () => Translate(i18n, "config.wildAnimals.reset.tooltip", "Clear configured override and use the JSON default for this animal."),
                getValue: () => false,
                setValue: value =>
                {
                    if (!value)
                        return;

                    var cfg = _config!.Get();
                    cfg.WildAnimalOverrides?.Remove(key);
                });
        });
}

 #if WEGG_DEV

            // ============================
            // Debug subpages
            // ============================

            layout.Page("debug.desertTomb", page =>
            {
                page.SectionLabel(() => Translate(i18n, "config.section.debug.desertTomb.tuningGeneration.label", "Tuning – Desert Tomb Generation"));

page.Separator(simple: true);
page.SectionLabel(() => Translate(i18n, "config.section.debug.desertTomb.force.label", "Force – Desert Tomb Handler"));
page.Bool(cfg => cfg.DebugForceDesertTombHandler, optionId: "debug.desertTomb.forceHandler", tooltip: true);
page.Bool(cfg => cfg.DebugForceDesertTombUnrestricted, optionId: "debug.desertTomb.forceUnrestricted", tooltip: true);


                page.Float(cfg => cfg.DebugKnobs.DesertTomb.Generation.LightingDarkChance, optionId: "debug.tuning.desertTomb.gen.lightingDarkChance", tooltip: true, min: 0f, max: 1f, interval: 0.01f);
                page.Float(cfg => cfg.DebugKnobs.DesertTomb.Generation.LightingDarkAltChance, optionId: "debug.tuning.desertTomb.gen.lightingDarkAltChance", tooltip: true, min: 0f, max: 1f, interval: 0.01f);
                page.Float(cfg => cfg.DebugKnobs.DesertTomb.Generation.LightingSpecialDarkChance, optionId: "debug.tuning.desertTomb.gen.lightingSpecialDarkChance", tooltip: true, min: 0f, max: 1f, interval: 0.01f);

                page.Int(cfg => cfg.DebugKnobs.DesertTomb.Generation.BreakablesBaseNextMaxExclusive, optionId: "debug.tuning.desertTomb.gen.breakablesBaseNextMaxExclusive", tooltip: true, min: 1, max: 50, interval: 1);
                page.Float(cfg => cfg.DebugKnobs.DesertTomb.Generation.BreakablesLuckScalar, optionId: "debug.tuning.desertTomb.gen.breakablesLuckScalar", tooltip: true, min: 0f, max: 100f, interval: 1f);
                page.Int(cfg => cfg.DebugKnobs.DesertTomb.Generation.BreakablesMapAreaDivisor, optionId: "debug.tuning.desertTomb.gen.breakablesMapAreaDivisor", tooltip: true, min: 5, max: 400, interval: 1);

                page.Float(cfg => cfg.DebugKnobs.DesertTomb.Generation.BreakablesEdgeTopChance, optionId: "debug.tuning.desertTomb.gen.breakablesEdgeTopChance", tooltip: true, min: 0f, max: 1f, interval: 0.01f);
                page.Int(cfg => cfg.DebugKnobs.DesertTomb.Generation.BreakablesPlacementGuardMax, optionId: "debug.tuning.desertTomb.gen.breakablesPlacementGuardMax", tooltip: true, min: 50, max: 10000, interval: 10);
                page.Float(cfg => cfg.DebugKnobs.DesertTomb.Generation.BreakablesPlaceChance, optionId: "debug.tuning.desertTomb.gen.breakablesPlaceChance", tooltip: true, min: 0f, max: 1f, interval: 0.01f);
                page.Float(cfg => cfg.DebugKnobs.DesertTomb.Generation.BreakablesClusterChance, optionId: "debug.tuning.desertTomb.gen.breakablesClusterChance", tooltip: true, min: 0f, max: 1f, interval: 0.01f);

                page.Int(cfg => cfg.DebugKnobs.DesertTomb.Generation.LightsMapAreaDivisor, optionId: "debug.tuning.desertTomb.gen.lightsMapAreaDivisor", tooltip: true, min: 50, max: 2000, interval: 10);
                page.Int(cfg => cfg.DebugKnobs.DesertTomb.Generation.LightsTargetMin, optionId: "debug.tuning.desertTomb.gen.lightsTargetMin", tooltip: true, min: 0, max: 200, interval: 1);
                page.Int(cfg => cfg.DebugKnobs.DesertTomb.Generation.LightsTargetMax, optionId: "debug.tuning.desertTomb.gen.lightsTargetMax", tooltip: true, min: 0, max: 300, interval: 1);
                page.Int(cfg => cfg.DebugKnobs.DesertTomb.Generation.LightsAttemptsMultiplier, optionId: "debug.tuning.desertTomb.gen.lightsAttemptsMultiplier", tooltip: true, min: 1, max: 500, interval: 1);

                page.Separator(simple: true);

                page.SectionLabel(() => Translate(i18n, "config.section.debug.desertTomb.tuningSpawning.label", "Tuning – Desert Tomb Spawning"));
                page.Float(cfg => cfg.DebugKnobs.DesertTomb.Spawning.GuardFraction, optionId: "debug.tuning.desertTomb.spawn.guardFraction", tooltip: true, min: 0f, max: 1f, interval: 0.01f);
                page.Float(cfg => cfg.DebugKnobs.DesertTomb.Spawning.WanderPackChance, optionId: "debug.tuning.desertTomb.spawn.wanderPackChance", tooltip: true, min: 0f, max: 1f, interval: 0.01f);

                page.Int(cfg => cfg.DebugKnobs.DesertTomb.Spawning.LeashRadiusMin, optionId: "debug.tuning.desertTomb.spawn.leashRadiusMin", tooltip: true, min: 0, max: 80, interval: 1);
                page.Int(cfg => cfg.DebugKnobs.DesertTomb.Spawning.LeashRadiusMax, optionId: "debug.tuning.desertTomb.spawn.leashRadiusMax", tooltip: true, min: 1, max: 120, interval: 1);

                page.Int(cfg => cfg.DebugKnobs.DesertTomb.Spawning.PatrolPointsMinInclusive, optionId: "debug.tuning.desertTomb.spawn.patrolPointsMinInclusive", tooltip: true, min: 1, max: 20, interval: 1);
                page.Int(cfg => cfg.DebugKnobs.DesertTomb.Spawning.PatrolPointsMaxExclusive, optionId: "debug.tuning.desertTomb.spawn.patrolPointsMaxExclusive", tooltip: true, min: 2, max: 40, interval: 1);

                page.Int(cfg => cfg.DebugKnobs.DesertTomb.Spawning.PatrolMaxAttempts, optionId: "debug.tuning.desertTomb.spawn.patrolMaxAttempts", tooltip: true, min: 10, max: 5000, interval: 10);
                page.Int(cfg => cfg.DebugKnobs.DesertTomb.Spawning.PatrolAttemptsPerCandidate, optionId: "debug.tuning.desertTomb.spawn.patrolAttemptsPerCandidate", tooltip: true, min: 1, max: 50, interval: 1);
                page.Int(cfg => cfg.DebugKnobs.DesertTomb.Spawning.PatrolMinSpacingTiles, optionId: "debug.tuning.desertTomb.spawn.patrolMinSpacingTiles", tooltip: true, min: 0, max: 20, interval: 1);

                page.Separator(simple: true);

                page.SectionLabel(() => Translate(i18n, "config.section.debug.desertTomb.tuningPopulation.label", "Tuning – Desert Tomb Population"));
                page.Float(cfg => cfg.DebugKnobs.DesertTomb.Population.InitialWaveFraction, optionId: "debug.tuning.desertTomb.pop.initialWaveFraction", tooltip: true, min: 0f, max: 1f, interval: 0.01f);
                page.Int(cfg => cfg.DebugKnobs.DesertTomb.Population.ReconcileIntervalTicks, optionId: "debug.tuning.desertTomb.pop.reconcileIntervalTicks", tooltip: true, min: 1, max: 600, interval: 1);
                page.Int(cfg => cfg.DebugKnobs.DesertTomb.Population.MaxSpawnsPerTick, optionId: "debug.tuning.desertTomb.pop.maxSpawnsPerTick", tooltip: true, min: 0, max: 50, interval: 1);
                page.Int(cfg => cfg.DebugKnobs.DesertTomb.Population.MinDistanceToPlayerTiles, optionId: "debug.tuning.desertTomb.pop.minDistanceToPlayerTiles", tooltip: true, min: 0, max: 30, interval: 1);
                page.Bool(cfg => cfg.DebugKnobs.DesertTomb.Population.AllowRespawnAfterInitialFill, optionId: "debug.tuning.desertTomb.pop.allowRespawnAfterInitialFill", tooltip: true);

                page.Separator(simple: true);

                page.SectionLabel(() => Translate(i18n, "config.section.debug.desertTomb.tuningLuckLoot.label", "Tuning – Desert Tomb Luck / Loot"));
                page.Float(cfg => cfg.DebugKnobs.DesertTomb.LootLuck.UnluckyDelta, optionId: "debug.tuning.desertTomb.luck.unluckyDelta", tooltip: true, min: -0.05f, max: 0f, interval: 0.0005f);
                page.Float(cfg => cfg.DebugKnobs.DesertTomb.LootLuck.LuckyDelta, optionId: "debug.tuning.desertTomb.luck.luckyDelta", tooltip: true, min: 0f, max: 0.05f, interval: 0.0005f);
                page.Float(cfg => cfg.DebugKnobs.DesertTomb.LootLuck.SuperLuckyDelta, optionId: "debug.tuning.desertTomb.luck.superLuckyDelta", tooltip: true, min: 0f, max: 0.5f, interval: 0.001f);

                page.Float(cfg => cfg.DebugKnobs.DesertTomb.LootLuck.SuperUnluckyMax, optionId: "debug.tuning.desertTomb.luck.superUnluckyMax", tooltip: true, min: -0.2f, max: 0f, interval: 0.005f);
                page.Float(cfg => cfg.DebugKnobs.DesertTomb.LootLuck.UnluckyMax, optionId: "debug.tuning.desertTomb.luck.unluckyMax", tooltip: true, min: -0.2f, max: 0f, interval: 0.005f);
                page.Float(cfg => cfg.DebugKnobs.DesertTomb.LootLuck.NeutralMax, optionId: "debug.tuning.desertTomb.luck.neutralMax", tooltip: true, min: 0f, max: 0.2f, interval: 0.005f);
                page.Float(cfg => cfg.DebugKnobs.DesertTomb.LootLuck.LuckyMax, optionId: "debug.tuning.desertTomb.luck.luckyMax", tooltip: true, min: 0f, max: 0.2f, interval: 0.005f);

                page.Float(cfg => cfg.DebugKnobs.DesertTomb.Breakables.DropRateMultiplierMax, optionId: "debug.tuning.desertTomb.breakables.dropRateMultiplierMax", tooltip: true, min: 0f, max: 100f, interval: 0.5f);
            });

            layout.Page("debug.spawning", page =>
            {
                _devJsonTuning!.BuildSpawnChanceModifiersPage(page);

                page.Separator(simple: true);
                _devJsonTuning.BuildSpawnParametersPage(page);
            });

            layout.Page("debug.weighting", page =>
            {
                page.SectionLabel(() => Translate(i18n, "config.section.debug.weighting.tuningEconomy.label", "Tuning – Economy Weighting"));
                page.Int(cfg => cfg.DebugKnobs.Weighting.HorizonDays, optionId: "debug.tuning.weighting.horizonDays", tooltip: true, min: 1, max: 60, interval: 1);
                page.Float(cfg => cfg.DebugKnobs.Weighting.DeluxeProbability, optionId: "debug.tuning.weighting.deluxeProbability", tooltip: true, min: 0f, max: 1f, interval: 0.01f);

                page.Float(cfg => cfg.DebugKnobs.Weighting.K, optionId: "debug.tuning.weighting.k", tooltip: true, min: 0f, max: 100000f, interval: 50f);
                page.Float(cfg => cfg.DebugKnobs.Weighting.Alpha, optionId: "debug.tuning.weighting.alpha", tooltip: true, min: 0f, max: 2f, interval: 0.01f);

                page.Float(cfg => cfg.DebugKnobs.Weighting.CapHigh, optionId: "debug.tuning.weighting.capHigh", tooltip: true, min: 0f, max: 100f, interval: 0.5f);
                page.Float(cfg => cfg.DebugKnobs.Weighting.CapLow, optionId: "debug.tuning.weighting.capLow", tooltip: true, min: 0f, max: 200f, interval: 0.5f);

                page.Float(cfg => cfg.DebugKnobs.Weighting.UnlockedFactor, optionId: "debug.tuning.weighting.unlockedFactor", tooltip: true, min: 0f, max: 2f, interval: 0.01f);
                page.Float(cfg => cfg.DebugKnobs.Weighting.HiddenNoAnchorPenalty, optionId: "debug.tuning.weighting.hiddenNoAnchorPenalty", tooltip: true, min: 0f, max: 2f, interval: 0.01f);
            });

            layout.Page("debug.monsters", page =>
            {
                page.SectionLabel(() => Translate(i18n, "config.section.debug.monsters.tuningPharaoh.label", "Tuning – Monsters (Pharaoh)"));
                page.Int(cfg => cfg.DebugKnobs.Monsters.Pharaoh.VisionTiles, optionId: "debug.tuning.monsters.pharaoh.visionTiles", tooltip: true, min: 0, max: 30, interval: 1);
                page.Int(cfg => cfg.DebugKnobs.Monsters.Pharaoh.CastRangeTiles, optionId: "debug.tuning.monsters.pharaoh.castRangeTiles", tooltip: true, min: 0, max: 30, interval: 1);
                page.Int(cfg => cfg.DebugKnobs.Monsters.Pharaoh.RewrapRangeTiles, optionId: "debug.tuning.monsters.pharaoh.rewrapRangeTiles", tooltip: true, min: 0, max: 30, interval: 1);

                page.Int(cfg => cfg.DebugKnobs.Monsters.Pharaoh.BaseCastCooldownMs, optionId: "debug.tuning.monsters.pharaoh.baseCastCooldownMs", tooltip: true, min: 0, max: 20000, interval: 50);
                page.Int(cfg => cfg.DebugKnobs.Monsters.Pharaoh.EnragedCastCooldownMs, optionId: "debug.tuning.monsters.pharaoh.enragedCastCooldownMs", tooltip: true, min: 0, max: 20000, interval: 50);
                page.Int(cfg => cfg.DebugKnobs.Monsters.Pharaoh.CastWindupMs, optionId: "debug.tuning.monsters.pharaoh.castWindupMs", tooltip: true, min: 0, max: 10000, interval: 50);
                page.Int(cfg => cfg.DebugKnobs.Monsters.Pharaoh.RecoverMs, optionId: "debug.tuning.monsters.pharaoh.recoverMs", tooltip: true, min: 0, max: 10000, interval: 50);

                page.Int(cfg => cfg.DebugKnobs.Monsters.Pharaoh.MicroUnstuckAfterTicks, optionId: "debug.tuning.monsters.pharaoh.microUnstuckAfterTicks", tooltip: true, min: 0, max: 600, interval: 1);
                page.Int(cfg => cfg.DebugKnobs.Monsters.Pharaoh.PathfindUnstuckAfterTicks, optionId: "debug.tuning.monsters.pharaoh.pathfindUnstuckAfterTicks", tooltip: true, min: 0, max: 600, interval: 1);

                page.Int(cfg => cfg.DebugKnobs.Monsters.Pharaoh.SandStepAfterTicks, optionId: "debug.tuning.monsters.pharaoh.sandStepAfterTicks", tooltip: true, min: 0, max: 600, interval: 1);
                page.Int(cfg => cfg.DebugKnobs.Monsters.Pharaoh.SandStepCooldownMs, optionId: "debug.tuning.monsters.pharaoh.sandStepCooldownMs", tooltip: true, min: 0, max: 30000, interval: 100);

                page.Int(cfg => cfg.DebugKnobs.Monsters.Pharaoh.LeashRadiusTiles, optionId: "debug.tuning.monsters.pharaoh.leashRadiusTiles", tooltip: true, min: 0, max: 60, interval: 1);
                page.Int(cfg => cfg.DebugKnobs.Monsters.Pharaoh.LeashTicksBeforeReturn, optionId: "debug.tuning.monsters.pharaoh.leashTicksBeforeReturn", tooltip: true, min: 0, max: 2000, interval: 5);

                page.Float(cfg => cfg.DebugKnobs.Monsters.Pharaoh.RewrapChance, optionId: "debug.tuning.monsters.pharaoh.rewrapChance", tooltip: true, min: 0f, max: 1f, interval: 0.01f);
                page.Int(cfg => cfg.DebugKnobs.Monsters.Pharaoh.EnrageCooldownCapMs, optionId: "debug.tuning.monsters.pharaoh.enrageCooldownCapMs", tooltip: true, min: 0, max: 5000, interval: 25);
                page.Int(cfg => cfg.DebugKnobs.Monsters.Pharaoh.ReviveTimerMax, optionId: "debug.tuning.monsters.pharaoh.reviveTimerMax", tooltip: true, min: 0, max: 5000, interval: 25);
                page.Int(cfg => cfg.DebugKnobs.Monsters.Pharaoh.HealEnraged, optionId: "debug.tuning.monsters.pharaoh.healEnraged", tooltip: true, min: 0, max: 1000, interval: 5);
                page.Int(cfg => cfg.DebugKnobs.Monsters.Pharaoh.HealNormal, optionId: "debug.tuning.monsters.pharaoh.healNormal", tooltip: true, min: 0, max: 1000, interval: 5);

                page.Separator(simple: true);

                page.SectionLabel(() => Translate(i18n, "config.section.debug.monsters.tuningSandi.label", "Tuning – Monsters (Sandi)"));
                page.Float(cfg => cfg.DebugKnobs.Monsters.Sandi.DiveChanceOnHit, optionId: "debug.tuning.monsters.sandi.diveChanceOnHit", tooltip: true, min: 0f, max: 1f, interval: 0.01f);
                page.Int(cfg => cfg.DebugKnobs.Monsters.Sandi.DiveDurationMs, optionId: "debug.tuning.monsters.sandi.diveDurationMs", tooltip: true, min: 0, max: 20000, interval: 50);
                page.Int(cfg => cfg.DebugKnobs.Monsters.Sandi.DiveCooldownMs, optionId: "debug.tuning.monsters.sandi.diveCooldownMs", tooltip: true, min: 0, max: 20000, interval: 50);

                page.Separator(simple: true);

                page.SectionLabel(() => Translate(i18n, "config.section.debug.monsters.tuningCrawler.label", "Tuning – Monsters (Crawler)"));
                page.Int(cfg => cfg.DebugKnobs.Monsters.Crawler.CurlTriggerDistanceTiles, optionId: "debug.tuning.monsters.crawler.curlTriggerDistanceTiles", tooltip: true, min: 0, max: 30, interval: 1);
                page.Int(cfg => cfg.DebugKnobs.Monsters.Crawler.CurlDurationMs, optionId: "debug.tuning.monsters.crawler.curlDurationMs", tooltip: true, min: 0, max: 20000, interval: 50);
                page.Int(cfg => cfg.DebugKnobs.Monsters.Crawler.CurlCooldownMs, optionId: "debug.tuning.monsters.crawler.curlCooldownMs", tooltip: true, min: 0, max: 20000, interval: 50);

                page.Separator(simple: true);

                page.SectionLabel(() => Translate(i18n, "config.section.debug.monsters.tuningHurricane.label", "Tuning – Monsters (Hurricane)"));
                page.Int(cfg => cfg.DebugKnobs.Monsters.Hurricane.AlignTiles, optionId: "debug.tuning.monsters.hurricane.alignTiles", tooltip: true, min: 0, max: 60, interval: 1);
                page.Int(cfg => cfg.DebugKnobs.Monsters.Hurricane.SuckDistanceTiles, optionId: "debug.tuning.monsters.hurricane.suckDistanceTiles", tooltip: true, min: 0, max: 30, interval: 1);
                page.Int(cfg => cfg.DebugKnobs.Monsters.Hurricane.SwirlSegmentMs, optionId: "debug.tuning.monsters.hurricane.swirlSegmentMs", tooltip: true, min: 0, max: 20000, interval: 50);
                page.Int(cfg => cfg.DebugKnobs.Monsters.Hurricane.ChargeMs, optionId: "debug.tuning.monsters.hurricane.chargeMs", tooltip: true, min: 0, max: 20000, interval: 50);
                page.Int(cfg => cfg.DebugKnobs.Monsters.Hurricane.SuckMs, optionId: "debug.tuning.monsters.hurricane.suckMs", tooltip: true, min: 0, max: 20000, interval: 50);

                page.Separator(simple: true);

                page.SectionLabel(() => Translate(i18n, "config.section.debug.monsters.tuningMatango.label", "Tuning – Monsters (Matango)"));
                page.Int(cfg => cfg.DebugKnobs.Monsters.Matango.VisionTiles, optionId: "debug.tuning.monsters.matango.visionTiles", tooltip: true, min: 0, max: 30, interval: 1);
                page.Int(cfg => cfg.DebugKnobs.Monsters.Matango.CastWindupMs, optionId: "debug.tuning.monsters.matango.castWindupMs", tooltip: true, min: 0, max: 10000, interval: 50);
                page.Int(cfg => cfg.DebugKnobs.Monsters.Matango.RecoverMs, optionId: "debug.tuning.monsters.matango.recoverMs", tooltip: true, min: 0, max: 10000, interval: 50);
                page.Int(cfg => cfg.DebugKnobs.Monsters.Matango.ShotCooldownMs, optionId: "debug.tuning.monsters.matango.shotCooldownMs", tooltip: true, min: 0, max: 20000, interval: 50);

                page.Separator(simple: true);

                page.SectionLabel(() => Translate(i18n, "config.section.debug.monsters.jsonMonsterDefinitions.label", "JSON – MonsterDefinitions (Dev)"));
                page.ParagraphDynamic(
                    text: () => Translate(i18n, "config.paragraph.debug.monsters.jsonMonsterDefinitions.notice.text", "Dev-only: edits assets/DesertTomb/MonsterDefinitions.json directly. Stats/Flags are written immediately. (Tip: restart or reload Tomb content to see changes.)"),
                    id: "json_monsters_notice"
                );
                page.ParagraphDynamic(
                    text: () => Translate(i18n, "config.paragraph.debug.monsters.jsonMonsterDefinitions.fields.text", "Fields: DifficultyAdd and SpeedAdd are additive adjustments; MaxHealthMultiplier/Add adjust max HP; DamageToFarmer sets base damage; ResilienceAdd adjusts resilience; IsBoss and HostAuthoritative are behavior flags."),
                    id: "json_monsters_fields"
                );

                foreach (var key in DevMonsterDefinitions.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase))
                {
                    var monsterKey = key;
                    var def = DevMonsterDefinitions[monsterKey] ?? new TombMonsterDefinitionJson();
                    def.Stats ??= new TombMonsterStatProfileJson();
                    def.Flags ??= new TombMonsterBehaviorFlagsJson();
                    DevMonsterDefinitions[monsterKey] = def;

                    page.Separator(simple: true, fieldId: $"json_monsters_sep_{NormalizeDevJsonKey(monsterKey)}");
                    page.SectionLabel(() => monsterKey);

                    page.Int(
                        optionId: $"JSON {monsterKey}: DifficultyAdd",
                        getValue: () => def.Stats!.DifficultyAdd ?? 0,
                        setValue: v => MutateDevMonsterDefinitions(() => def.Stats!.DifficultyAdd = v),
                        tooltip: false,
                        min: -10,
                        max: 50,
                        interval: 1,
                        fieldId: $"json_monsters_{NormalizeDevJsonKey(monsterKey)}_difficultyAdd"
                    );

                    page.Int(
                        optionId: $"JSON {monsterKey}: SpeedAdd",
                        getValue: () => def.Stats!.SpeedAdd ?? 0,
                        setValue: v => MutateDevMonsterDefinitions(() => def.Stats!.SpeedAdd = v),
                        tooltip: false,
                        min: -10,
                        max: 20,
                        interval: 1,
                        fieldId: $"json_monsters_{NormalizeDevJsonKey(monsterKey)}_speedAdd"
                    );

                    page.Float(
                        optionId: $"JSON {monsterKey}: MaxHealthMultiplier",
                        getValue: () => def.Stats!.MaxHealthMultiplier ?? 1f,
                        setValue: v => MutateDevMonsterDefinitions(() => def.Stats!.MaxHealthMultiplier = Math.Max(0f, v)),
                        tooltip: false,
                        min: 0f,
                        max: 10f,
                        interval: 0.05f,
                        fieldId: $"json_monsters_{NormalizeDevJsonKey(monsterKey)}_maxHealthMultiplier"
                    );

                    page.Int(
                        optionId: $"JSON {monsterKey}: MaxHealthAdd",
                        getValue: () => def.Stats!.MaxHealthAdd ?? 0,
                        setValue: v => MutateDevMonsterDefinitions(() => def.Stats!.MaxHealthAdd = v),
                        tooltip: false,
                        min: -500,
                        max: 5000,
                        interval: 10,
                        fieldId: $"json_monsters_{NormalizeDevJsonKey(monsterKey)}_maxHealthAdd"
                    );

                    page.Int(
                        optionId: $"JSON {monsterKey}: DamageToFarmer",
                        getValue: () => def.Stats!.DamageToFarmer ?? 0,
                        setValue: v => MutateDevMonsterDefinitions(() => def.Stats!.DamageToFarmer = Math.Max(0, v)),
                        tooltip: false,
                        min: 0,
                        max: 500,
                        interval: 1,
                        fieldId: $"json_monsters_{NormalizeDevJsonKey(monsterKey)}_damageToFarmer"
                    );

                    page.Int(
                        optionId: $"JSON {monsterKey}: ResilienceAdd",
                        getValue: () => def.Stats!.ResilienceAdd ?? 0,
                        setValue: v => MutateDevMonsterDefinitions(() => def.Stats!.ResilienceAdd = v),
                        tooltip: false,
                        min: -50,
                        max: 100,
                        interval: 1,
                        fieldId: $"json_monsters_{NormalizeDevJsonKey(monsterKey)}_resilienceAdd"
                    );

                    page.Bool(
                        optionId: $"JSON {monsterKey}: IsBoss",
                        getValue: () => def.Flags!.IsBoss ?? false,
                        setValue: v => MutateDevMonsterDefinitions(() => def.Flags!.IsBoss = v),
                        tooltip: false,
                        fieldId: $"json_monsters_{NormalizeDevJsonKey(monsterKey)}_isBoss"
                    );

                    page.Bool(
                        optionId: $"JSON {monsterKey}: HostAuthoritative",
                        getValue: () => def.Flags!.HostAuthoritative ?? true,
                        setValue: v => MutateDevMonsterDefinitions(() => def.Flags!.HostAuthoritative = v),
                        tooltip: false,
                        fieldId: $"json_monsters_{NormalizeDevJsonKey(monsterKey)}_hostAuthoritative"
                    );
                }
            });

            // Debug hub page
            layout.Page("debug", page =>
            {
                page.Bool(cfg => cfg.DesertTombPharaohTrackerEnabled, optionId: "debug.desertTomb.pharaohTracker", tooltip: true);
                page.Bool(cfg => cfg.DesertTombDebugLogging, optionId: "debug.desertTomb.traceLogging", tooltip: true);
                page.Bool(cfg => cfg.DebugForceDesertTombHandler, optionId: "debug.desertTomb.forceHandler", tooltip: true);
                page.Bool(cfg => cfg.DebugForceDesertTombUnrestricted, optionId: "debug.desertTomb.forceUnrestricted", tooltip: true);
                page.Bool(cfg => cfg.AnimalNotesDebugMode, optionId: "debug.animalNotes.debugMode", tooltip: true);

                page.Separator(simple: true);
                page.Link("debug.desertTomb", tooltip: true);
                page.Link("debug.spawning", tooltip: true);
                page.Link("debug.weighting", tooltip: true);
                page.Link("debug.monsters", tooltip: true);
            });

 #endif

        }, force: force, rebuildOnSaveLoaded: true, rebuildOnReturnedToTitle: true); // End of _gmcmHost.RebuildLayoutSaveAware
    } // End of RebuildGmcmLayout method

    private void BuildAnimalProgressionPage(GmcmPageBuilder<ModConfig> page, I18TranslationHelper i18n)
    {
        if (!Context.IsWorldReady)
        {
            page.ParagraphDynamic(
                () => Translate(i18n, "config.paragraph.animalProgression.loadSave.text", "Load a save to edit per-animal progression settings."),
                id: "animalProgression_loadSave");
            return;
        }

        Dictionary<string, FarmAnimalData> farmAnimals;
        try
        {
            farmAnimals = Helper.GameContent.Load<Dictionary<string, FarmAnimalData>>("Data/FarmAnimals");
        }
        catch (Exception ex)
        {
            var smapiLog = new SmapiLog(Monitor);
            smapiLog.LogScoped(ScopedLogCatalog.ModWeCategory.Content, $"Failed to load Data/FarmAnimals for GMCM Animal Progression page: {ex}", LogLevel.Error);
            page.ParagraphDynamic(
                () => Translate(i18n, "config.paragraph.animalProgression.loadFailed.text", "Couldn't load Data/FarmAnimals. Try again after loading a save, or check your SMAPI log."),
                id: "animalProgression_loadFailed");
            return;
        }

        if (farmAnimals.Count == 0)
        {
            page.ParagraphDynamic(
                () => Translate(i18n, "config.paragraph.animalProgression.empty.text", "No farm animals were found in Data/FarmAnimals."),
                id: "animalProgression_empty");
            return;
        }

        var config = _config!.Get();
        config.AnimalProgressionMode ??= new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        var animals = farmAnimals
            .OrderBy(pair => pair.Value?.DisplayName?.ToString() ?? pair.Key, StringComparer.OrdinalIgnoreCase)
            .ThenBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase);

        foreach (var (animalId, farmAnimal) in animals)
        {
            var defaultEnabled = AnimalShopProgressionPolicy.DefaultEnabled(animalId);
            var elementId = $"animalProgression.{animalId}";

            page.BoolLabel(
                elementId: elementId,
                name: () => BuildAnimalOptionName(animalId, farmAnimal),
                tooltip: () => BuildAnimalOptionTooltip(i18n, animalId, defaultEnabled),
                getValue: () => config.AnimalProgressionMode.TryGetValue(animalId, out var configured) ? configured : defaultEnabled,
                setValue: value =>
                {
                    if (value == defaultEnabled)
                        config.AnimalProgressionMode.Remove(animalId);
                    else
                        config.AnimalProgressionMode[animalId] = value;
                });
        }
    }

    private static string BuildAnimalOptionName(string animalId, FarmAnimalData? farmAnimal)
    {
        var displayName = farmAnimal?.DisplayName?.ToString();
        if (string.IsNullOrWhiteSpace(displayName) || string.Equals(displayName, animalId, StringComparison.OrdinalIgnoreCase))
            return animalId;

        return $"{displayName} ({animalId})";
    }

    private static string BuildAnimalOptionTooltip(I18TranslationHelper i18n, string animalId, bool defaultEnabled)
    {
        var defaultState = defaultEnabled
            ? Translate(i18n, "config.animalProgression.state.enabled", "Enabled")
            : Translate(i18n, "config.animalProgression.state.disabled", "Disabled");

        return Translate(
            i18n,
            "config.animalProgression.entry.tooltip",
            $"Gate this animal behind Animal Notes progression. Default: {defaultState}. Key: {animalId}.",
            defaultState,
            animalId);
    }

    private static string Translate(I18TranslationHelper i18n, string key, string fallback, params object[] tokens)
    {
        var translated = tokens.Length == 0
            ? i18n.GetString(key)
            : i18n.GetString(key, tokens);

        if (!string.IsNullOrWhiteSpace(translated) && !string.Equals(translated, key, StringComparison.OrdinalIgnoreCase))
            return translated;

        return tokens.Length == 0 ? fallback : string.Format(fallback, tokens);
    }


private static string BuildWildAnimalPageId(string wildAnimalKey)
    => $"wildAnimals.{Mikew.NestCore.Integrations.Gmcm.GmcmFieldIds.Normalize(wildAnimalKey)}";

private static bool GetWildAnimalEffectiveEnabled(ModConfig cfg, string wildAnimalKey, bool defaultEnabled)
{
    if (cfg.WildAnimalOverrides is not null
        && cfg.WildAnimalOverrides.TryGetValue(wildAnimalKey, out var ov)
        && ov?.Enabled is not null)
        return ov.Enabled.Value;

    return defaultEnabled;
}

private static string GetWildAnimalEnabledSource(ModConfig cfg, string wildAnimalKey, bool defaultEnabled)
{
    if (cfg.WildAnimalOverrides is not null
        && cfg.WildAnimalOverrides.TryGetValue(wildAnimalKey, out var ov)
        && ov?.Enabled is not null)
        return "override";

    return "default";
}

    
private void OnConfigUpdated(object? sender, ModConfig _)
{
    SyncApiBirthTextMode();
    TryInstallDesertTombHarmonyPatches(reason: "config updated");
}

private void SyncApiBirthTextMode()
    {
        if (_config is null || _api is null)
            return;

        _api.SetBirthTextMode(_config.Get().BirthText);
    }

    
    private void TryInstallDesertTombHarmonyPatches(string reason, bool force = false)
    {
        if (_config is null)
            return;

        var cfg = _config.Get();

#if WEGG_DEV
        var debugForce = cfg.DebugForceDesertTombHandler;
        var debugUnrestricted = cfg.DebugForceDesertTombUnrestricted;
#else
        var debugForce = false;
        var debugUnrestricted = false;
#endif

        var shouldInstall = force || cfg.DesertTombModuleEnabled || cfg.DesertTombForcePatch || debugForce || debugUnrestricted;

        Monitor.Log(
            $"[DesertTomb][Harmony] shouldInstall={shouldInstall} force={force} reason={reason} enabled={cfg.DesertTombModuleEnabled} forcePatch={cfg.DesertTombForcePatch} debugForce={debugForce} debugUnrestricted={debugUnrestricted}",
            LogLevel.Info);

        if (!shouldInstall)
            return;

        if (_harmony is null)
        {
            Monitor.Log("[DesertTomb][Harmony] Harmony bootstrap not initialized yet; cannot install patches.", LogLevel.Warn);
            return;
        }

        if (_desertTombHarmonyInstalled)
        {
            Monitor.Log("[DesertTomb][Harmony] Patches already installed (skipping).", LogLevel.Info);
            LogDesertTombHarmonyPatchStatus();
            return;
        }

        var runtime = EnsureDesertTombRuntime();
        if (runtime is null)
        {
            Monitor.Log("[DesertTomb][Harmony] DesertTombRuntime not available; cannot install patches.", LogLevel.Warn);
            return;
        }

        try
        {
            DesertTombMineShaftPatches.Install(_harmony, runtime);
            _desertTombHarmonyInstalled = true;
            Monitor.Log("[DesertTomb][Harmony] Installed DesertTomb Harmony patches successfully.", LogLevel.Info);
        }
        catch (Exception ex)
        {
            Monitor.Log($"[DesertTomb][Harmony] Failed to install DesertTomb Harmony patches: {ex}", LogLevel.Error);
            return;
        }

        LogDesertTombHarmonyPatchStatus();
    }

    private DesertTombRuntime? EnsureDesertTombRuntime()
    {
        if (_desertTombRuntime is not null)
            return _desertTombRuntime;

        if (_config is null)
            return null;

        var log = _contentLog ?? new SmapiLog(Monitor);

        // Ensure data loader exists for JSON-backed definitions.
        _contentDataLoader ??= new ConfiguredModDataLoader(new ModDataLoader(JsonFileReaderFactory.Create(Helper), log), _config!, log);

        var reader = JsonFileReaderFactory.Create(Helper);

        var monsterDefinitions = new TombMonsterDefinitions(reader, log);
        var dropTables = new TombDropTables(reader, log);
        var dropEngine = new TombDropTableEngine();

        var monsterFactory = new DesertTombMonsterFactory(monsterDefinitions, dropTables, dropEngine, log);

        var spawnExecutor = new DesertTombSpawnExecutor(monsterFactory, log);

        // Generator currently doesn't depend on SpawnConfiguration values (kept for future expansion).
        var generator = new DesertTombLevelGenerator(new SpawnConfiguration());

        _desertTombRuntime = new DesertTombRuntime(
            config: _config,
            getStep: () => _contentProgression?.GetCurrentStep() ?? ProgressionRules.MaxStep,
            log: log,
            isMainPlayer: () => Context.IsMainPlayer,
            generator: generator,
            spawnExecutor: spawnExecutor,
            diagnostics: null
        );

        return _desertTombRuntime;
    }

    private void LogDesertTombHarmonyPatchStatus()
    {
        if (_harmony is null)
            return;

        var harmonyId = _harmony.Id;

        LogPatchInfo(harmonyId, typeof(StardewValley.Locations.MineShaft), "loadLevel");
        LogPatchInfo(harmonyId, typeof(StardewValley.Locations.MineShaft), "chooseLevelType");
        LogPatchInfo(harmonyId, typeof(StardewValley.Locations.MineShaft), "populateLevel");
        LogPatchInfo(harmonyId, typeof(StardewValley.Locations.MineShaft), nameof(StardewValley.Locations.MineShaft.monsterDrop));

        LogPatchInfo(harmonyId, typeof(StardewValley.Objects.BreakableContainer), nameof(StardewValley.Objects.BreakableContainer.performToolAction));
        LogPatchInfo(harmonyId, typeof(StardewValley.Game1), nameof(StardewValley.Game1.createItemDebris));
    }

    private void LogPatchInfo(string harmonyId, Type type, string methodName)
    {
        var original = AccessTools.Method(type, methodName);
        if (original is null)
        {
            Monitor.Log($"[DesertTomb][Harmony] PatchStatus: {type.FullName}.{methodName} original=<null>", LogLevel.Info);
            return;
        }

        var info = Harmony.GetPatchInfo(original);
        if (info is null)
        {
            Monitor.Log($"[DesertTomb][Harmony] PatchStatus: {type.FullName}.{methodName} patched=<no>", LogLevel.Info);
            return;
        }

        var byUsPrefix = info.Prefixes?.Count(p => p.owner == harmonyId) ?? 0;
        var byUsPostfix = info.Postfixes?.Count(p => p.owner == harmonyId) ?? 0;
        var byUsTranspiler = info.Transpilers?.Count(p => p.owner == harmonyId) ?? 0;
        var byUsFinalizer = info.Finalizers?.Count(p => p.owner == harmonyId) ?? 0;

        Monitor.Log(
            $"[DesertTomb][Harmony] PatchStatus: {type.FullName}.{methodName} patched=<yes> ours(prefix={byUsPrefix}, postfix={byUsPostfix}, transpiler={byUsTranspiler}, finalizer={byUsFinalizer}) total(prefix={info.Prefixes?.Count ?? 0}, postfix={info.Postfixes?.Count ?? 0}, transpiler={info.Transpilers?.Count ?? 0}, finalizer={info.Finalizers?.Count ?? 0})",
            LogLevel.Info);
    }

private void InstallHarmonyBootstrap()
    {
        if (_harmony is not null)
            return;

        _harmony = new Harmony($"{Constants.ModId}.harmony");
    }

    private void InitializeModAssetHandler()
    {
        _assetHandler = new ModAssetHandler(Constants.ModId, Monitor);
        _assetHandler.AddAssets(
            new()
            {
                new(
                    enumAssetType: typeof(WeggObjectTexture),
                    enumAssetFolder: WeggAssetFolder.objects,
                    fileExt: ModAssetHandler.FileExtension.Png,
                    gamePath: $"Mods/{Constants.ModId}/objects"
                ),
                new(
                    enumAssetType: typeof(WeggBigCraftableTexture),
                    enumAssetFolder: WeggAssetFolder.bigcraftables,
                    fileExt: ModAssetHandler.FileExtension.Png,
                    gamePath: $"Mods/{Constants.ModId}/bigcraftables"
                ),
                new(
                    enumAssetType: typeof(WeggMiscTexture),
                    enumAssetFolder: WeggAssetFolder.misc,
                    fileExt: ModAssetHandler.FileExtension.Png,
                    gamePath: $"Mods/{Constants.ModId}/misc"
                ),
                new(
                    enumAssetType: typeof(WeggMineMap),
                    enumAssetFolder: WeggAssetFolder.mines,
                    fileExt: ModAssetHandler.FileExtension.Tmx,
                    gamePath: "Maps/Mines"
                ),
                new(
                    enumAssetType: typeof(WeggMineTexture),
                    enumAssetFolder: WeggAssetFolder.mines,
                    fileExt: ModAssetHandler.FileExtension.Png,
                    gamePath: "Maps/Mines"
                )
            }
        );

        _assetHandler.SetUpAssets();
    }

    private void InitializeContentPipeline()
    {
        if (_config is null)
            return;

        _contentLog = new SmapiLog(Monitor);
        _contentDataLoader = new ConfiguredModDataLoader(new ModDataLoader(JsonFileReaderFactory.Create(Helper), _contentLog), _config!, _contentLog);

        var saveData = new SmapiSaveDataStore(Helper, Monitor);
        var game = new GameStateReader();
        var quests = new GameQuestService();
        var mail = new GameMailService();
        var recipes = new GameRecipeService();
        var unlockFacts = new GameUnlockFactsProvider(game, mail, quests);

        _contentProgression = new ProgressionService(
            store: saveData,
            config: _config,
            facts: unlockFacts,
            quests: quests,
            mail: mail,
            recipes: recipes,
            log: _contentLog,
            invalidateAssets: InvalidateGameAssets);

        var chosenEggAssignments = new ChosenEggAssignments(_contentDataLoader, Helper.ModRegistry, _contentLog);
        _contentPipeline = new AssetPipelineHost<ContentContext>(Helper, Monitor, CreateContentContext);
        if (_assetHandler is not null)
            _contentPipeline.RegisterModAssets(_assetHandler, order: -100);

        _contentPipeline.RegisterEditor(new MonstersEditor(), order: 10, shouldApply: (ctx, _) => ctx.Step >= ProgressionStep.PartI);
        _contentPipeline.RegisterEditor(new FarmAnimalsEditor(chosenEggAssignments), order: 20, shouldApply: (ctx, _) => ctx.Step >= ProgressionStep.PartI);
        _contentPipeline.RegisterEditor(new ObjectsEditor(), order: 30, shouldApply: (ctx, _) => ctx.Step >= ProgressionStep.PartI);
        _contentPipeline.RegisterEditor(new BigCraftablesEditor(), order: 40, shouldApply: (ctx, _) => ctx.Step >= ProgressionStep.PartI);
        _contentPipeline.RegisterEditor(new MachinesEditor(), order: 50, shouldApply: (ctx, _) => ctx.Step >= ProgressionStep.PartI);
        _contentPipeline.RegisterEditor(new CraftingRecipesEditor(), order: 60, shouldApply: (ctx, _) => ctx.Step >= ProgressionStep.PartI);
        _contentPipeline.RegisterEditor(new MailEditor(), order: 70, shouldApply: (ctx, _) => ctx.Step >= ProgressionStep.PartI);
        _contentPipeline.RegisterEditor(new QuestEditor(), order: 80, shouldApply: (ctx, _) => ctx.Step >= ProgressionStep.PartI);
        _contentPipeline.RegisterEditor(new SpecialOrdersEditor(), order: 90, shouldApply: (ctx, _) => ctx.Step >= ProgressionStep.PartI);
        _contentPipeline.RegisterEditor(new WildEggIncubatorTextureEditor(), order: 100, shouldApply: (ctx, _) => ctx.Step >= ProgressionStep.PartI);
        _contentPipeline.Attach();
    }

    private ContentContext CreateContentContext()
    {
        var config = _config?.Get() ?? new ModConfig();
        var log = _contentLog ?? new SmapiLog(Monitor);
        var i18n = new I18TranslationHelper(Helper);
        var configProvider = _config ?? new SmapiConfigProvider<ModConfig>(Helper, Monitor);
        var dataLoader = _contentDataLoader ?? new ConfiguredModDataLoader(new ModDataLoader(JsonFileReaderFactory.Create(Helper), log), configProvider, log);
        return new ContentContext(
            helper: Helper,
            log: log,
            i18n: i18n,
            config: config,
            step: GetContentProgressionStep(),
            modData: dataLoader.Load());
    }

    private ProgressionStep GetContentProgressionStep()
    {
        if (_config is null)
            return ProgressionStep.PartI;

        if (_contentProgression is not null)
            return _contentProgression.GetCurrentStep();

        return _config.Get().ProgressionMode ? ProgressionStep.PartI : ProgressionRules.MaxStep;
    }

    private void InvalidateGameAssets()
    {
        Helper.GameContent.InvalidateCache("Data/FarmAnimals");
        Helper.GameContent.InvalidateCache("Data/Objects");
        Helper.GameContent.InvalidateCache("Data/Machines");
        Helper.GameContent.InvalidateCache("Data/Mail");
        Helper.GameContent.InvalidateCache("Data/Quests");
        Helper.GameContent.InvalidateCache("Data/CraftingRecipes");
        Helper.GameContent.InvalidateCache("Data/SpecialOrders");
    }

    private void RegisterConsoleCommands(IModHelper helper)
    {
        if (_config is null)
            return;

        var log = new SmapiLog(Monitor);
        var i18n = new I18TranslationHelper(helper);
        var saveData = new SmapiSaveDataStore(helper, Monitor);
        var game = new GameStateReader();
        var world = new WorldLocations();
        var items = new GameItemRegistry();
        var tileFinder = new GameTileFinder();
        var placer = new GameObjectPlacer();
        var histogram = new EggsInWild.Application.Spawning.Diagnostics.TileSearchHistogram();

        var quests = new GameQuestService();
        var mail = new GameMailService();
        var recipes = new GameRecipeService();
        var unlockFacts = new GameUnlockFactsProvider(game, mail, quests);

        var progression = new EggsInWild.Application.Progression.ProgressionService(
            store: saveData,
            config: _config,
            facts: unlockFacts,
            quests: quests,
            mail: mail,
            recipes: recipes,
            log: log,
            invalidateAssets: InvalidateGameAssets);

        var dataLoader = new ConfiguredModDataLoader(new ModDataLoader(JsonFileReaderFactory.Create(helper), log), _config!, log);
        var mods = new EggsInWild.Infrastructure.Mods.SmapiModAvailability(helper.ModRegistry);
        var spawnWeather = new EggsInWild.Application.Spawning.SpawnWeatherHistory(saveData, log);
        var conditions = new EggsInWild.Application.Spawning.DefaultAreaConditionsEvaluator(game, spawnWeather);
        var planner = new EggsInWild.Application.Spawning.SpawnPlanner(
            world,
            mods,
            conditions,
            log);
        var spawnService = new EggsInWild.Application.Spawning.EggSpawnService(world, placer, tileFinder, log, histogram);
        var spawner = new EggsInWild.Application.Spawning.WildEggSpawnOrchestrator(dataLoader, game, planner, spawnService, log);

        var animalNotes = new EggsInWild.Application.AnimalNotes.AnimalNotesOrchestrator(
            store: saveData,
            dataLoader: dataLoader,
            game: game,
            i18n: i18n,
            debugMode: _config.Get().AnimalNotesDebugMode,
            warnAfterDays: _config.Get().AnimalNotesWarnAfterDays,
            placementRadius: _config.Get().AnimalNotesPlacementRadius,
            mail: mail,
            quests: quests,
            placer: placer,
            progression: progression,
            log: log,
            getStep: progression.GetCurrentStep,
            isMainPlayer: () => Context.IsMainPlayer,
            sendQueueMailMessage: null);

        var animalNotesValidator = new EggsInWild.Application.AnimalNotes.AnimalNotesStartupValidator(
            dataLoader,
            log,
            progression.GetCurrentStep);

        var verifier = new WeggVerifier(helper, saveData, _config, dataLoader, log);

        var facade = new ModFacade(
            store: saveData,
            config: _config,
            log: log,
            isWorldReady: () => Context.IsWorldReady,
            isMainPlayer: () => Context.IsMainPlayer,
            reloadAll: () =>
            {
                _config.Reload();
                InvalidateGameAssets();
            },
            progression: progression,
            spawner: spawner,
            animalNotes: animalNotes,
            animalNotesValidator: animalNotesValidator,
            world: world,
            items: items,
            game: game,
            tileFinder: tileFinder,
            placer: placer,
            dataLoader: dataLoader,
            mods: mods,
            verifier: verifier,
            histogram: histogram
#if WEGG_DEV
            ,
            forceInstallDesertTombPatches: () =>
            {
                var wasInstalled = _desertTombHarmonyInstalled;
                TryInstallDesertTombHarmonyPatches(reason: "console command wegg tomb force_patches", force: true);
                var isInstalled = _desertTombHarmonyInstalled;

                if (isInstalled && !wasInstalled)
                    return new EggsInWild.Application.Facade.FacadeResult(true, "DesertTomb Harmony patches installed.");
                if (isInstalled && wasInstalled)
                    return new EggsInWild.Application.Facade.FacadeResult(true, "DesertTomb Harmony patches already installed.");
                return new EggsInWild.Application.Facade.FacadeResult(false, "DesertTomb Harmony patches not installed (see log).");
            }
#endif

        );

        var dispatcher = new WeggCommandDispatcher(facade, i18n);
        var commands = new WeggConsoleCommands(log, i18n, dispatcher);
        commands.Register(helper);
    }

    private static IReadOnlyList<string> ParseCsvKeyList(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return Array.Empty<string>();

        var outList = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var part in raw.Split(new[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var t = part.Trim();
            if (t.Length == 0)
                continue;

            if (seen.Add(t))
                outList.Add(t);
        }

        return outList;
    }

    private static bool SequenceEqualIgnoreCase(IEnumerable<string> left, IEnumerable<string> right)
    {
        if (ReferenceEquals(left, right))
            return true;

        left ??= Array.Empty<string>();
        right ??= Array.Empty<string>();

        return left.SequenceEqual(right, StringComparer.OrdinalIgnoreCase);
    }

    private static void EnsureNoLocalNestCoreDuplicates(IModHelper helper)
    {
        var localNestCore = System.IO.Path.Combine(helper.DirectoryPath, "NestCore.dll");
        if (System.IO.File.Exists(localNestCore))
            throw new InvalidOperationException("Local NestCore.dll detected under EggsInWild. Remove it and rely on the insNestCoreed NestCore mod.");
    }
}
