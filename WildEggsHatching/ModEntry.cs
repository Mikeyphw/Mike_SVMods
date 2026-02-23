using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Mikew.BirdsOfAFeather.Helpers;
using Mikew.BirdsOfAFeather.Helpers.Config;
using Mikew.BirdsOfAFeather.Helpers.Logging;
using Mikew.BirdsOfAFeather.Helpers.SaveData;
using Mikew.BirdsOfAFeather.Integrations.Gmcm;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley.GameData.FarmAnimals;
using WildEggsHatching.Application.Facade;
using WildEggsHatching.Core.ShopProgression;
using WildEggsHatching.Infrastructure.Api;
using WildEggsHatching.Infrastructure.Commands;
using WildEggsHatching.Infrastructure.Config;
using WildEggsHatching.Infrastructure.Diagnostics;
using WildEggsHatching.Infrastructure.Game;
using WildEggsHatching.Infrastructure.ModData;

namespace WildEggsHatching;

public sealed class ModEntry : Mod
{
    private SmapiConfigProvider<ModConfig>? _config;
    private GmcmConfigHost<ModConfig>? _gmcmHost;
    private WildEggsApi? _api;
    private Harmony? _harmony;

    public override void Entry(IModHelper helper)
    {
        _config = new SmapiConfigProvider<ModConfig>(helper, Monitor);
        _config.ConfigUpdated += (_, __) => SyncApiBirthTextMode();

        EnsureNoLocalBoafDuplicates(helper);
        RegisterConsoleCommands(helper);

        helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        helper.Events.GameLoop.DayStarted += OnDayStarted;
        helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;

        InstallHarmonyBootstrap();
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

        RebuildGmcmLayout(force: true);
    }

    private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        SyncApiBirthTextMode();
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        RebuildGmcmLayout(force: true);
    }

    private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
    {
        RebuildGmcmLayout(force: true);
    }

    private void RebuildGmcmLayout(bool force)
    {
        if (_gmcmHost is null)
            return;

        var i18n = new I18TranslationHelper(Helper);

        _gmcmHost.RebuildLayout(layout =>
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
                root.Link("debug", tooltip: true);
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
                    () => Translate(i18n, "config.animalNotes.progressionOverrides.paragraph", "Per-animal progression overrides are managed on the Animal Progression page."),
                    id: "animalNotes_progression_hint");
                page.Link("animalProgression", tooltip: true);
            });

            layout.Page("animalProgression", page =>
            {
                page.ParagraphDynamic(
                    () => Translate(i18n, "config.animalProgression.description.paragraph", "Configure per-animal progression gating."),
                    id: "animalProgression_intro");
                BuildAnimalProgressionPage(page, i18n);
            });

            layout.Page("debug", page =>
            {
                page.Bool(cfg => cfg.DesertTombPharaohTrackerEnabled, optionId: "debug.desertTomb.pharaohTracker", tooltip: true);
                page.Bool(cfg => cfg.DesertTombDebugLogging, optionId: "debug.desertTomb.traceLogging", tooltip: true);
                page.Bool(cfg => cfg.AnimalNotesDebugMode, optionId: "debug.animalNotes.debugMode", tooltip: true);
            });
        }, force: force);
    }

    private void BuildAnimalProgressionPage(GmcmPageBuilder<ModConfig> page, I18TranslationHelper i18n)
    {
        if (!Context.IsWorldReady)
        {
            page.ParagraphDynamic(
                () => Translate(i18n, "config.animalProgression.loadSave.paragraph", "Load a save to edit per-animal progression settings."),
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
            Monitor.Log($"Failed to load Data/FarmAnimals for GMCM Animal Progression page: {ex}", LogLevel.Error);
            page.ParagraphDynamic(
                () => Translate(i18n, "config.animalProgression.loadFailed.paragraph", "Couldn't load Data/FarmAnimals. Try again after loading a save, or check your SMAPI log."),
                id: "animalProgression_loadFailed");
            return;
        }

        if (farmAnimals.Count == 0)
        {
            page.ParagraphDynamic(
                () => Translate(i18n, "config.animalProgression.empty.paragraph", "No farm animals were found in Data/FarmAnimals."),
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

    private void SyncApiBirthTextMode()
    {
        if (_config is null || _api is null)
            return;

        _api.SetBirthTextMode(_config.Get().BirthText);
    }

    private void InstallHarmonyBootstrap()
    {
        if (_harmony is not null)
            return;

        _harmony = new Harmony($"{Constants.ModId}.harmony");
    }

    private void RegisterConsoleCommands(IModHelper helper)
    {
        if (_config is null)
            return;

        var log = new SmapiLog(Monitor);
        var i18n = new I18TranslationHelper(helper);
        var saveData = new SmapiSaveDataStore(helper, Monitor);
        var dataLoader = new ModDataLoader(new SmapiJsonFileReader(helper), log);
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
                helper.GameContent.InvalidateCache("Data/FarmAnimals");
                helper.GameContent.InvalidateCache("Data/Objects");
                helper.GameContent.InvalidateCache("Data/Machines");
                helper.GameContent.InvalidateCache("Data/Mail");
                helper.GameContent.InvalidateCache("Data/Quests");
                helper.GameContent.InvalidateCache("Data/CraftingRecipes");
                helper.GameContent.InvalidateCache("Data/SpecialOrders");
            },
            progression: null,
            spawner: null,
            animalNotes: null,
            animalNotesValidator: null,
            world: new WorldLocations(),
            items: new GameItemRegistry(),
            game: new GameStateReader(),
            tileFinder: new GameTileFinder(),
            placer: new GameObjectPlacer(),
            verifier: verifier);

        var dispatcher = new WeggCommandDispatcher(facade, i18n);
        var commands = new WeggConsoleCommands(log, i18n, dispatcher);
        commands.Register(helper);
    }

    private static void EnsureNoLocalBoafDuplicates(IModHelper helper)
    {
        var localBoaf = System.IO.Path.Combine(helper.DirectoryPath, "BirdsOfAFeather.dll");
        if (System.IO.File.Exists(localBoaf))
            throw new InvalidOperationException("Local BirdsOfAFeather.dll detected under WildEggsHatching. Remove it and rely on the installed BOAF mod.");
    }
}
