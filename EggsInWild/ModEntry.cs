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
using EggsInWild.Infrastructure.ModData;
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

    public override void Entry(IModHelper helper)
    {
        _config = new SmapiConfigProvider<ModConfig>(helper, Monitor);
        _config.ConfigUpdated += (_, __) => SyncApiBirthTextMode();

        EnsureNoLocalNestCoreDuplicates(helper);
        RegisterConsoleCommands(helper);

        helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        helper.Events.GameLoop.DayStarted += OnDayStarted;
        helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;

        InitializeModAssetHandler();
        InitializeContentPipeline();

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
        _contentProgression?.OnDayStarted();
        SyncApiBirthTextMode();
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        _contentProgression?.OnSaveLoaded();
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
            var smapiLog = new SmapiLog(Monitor);
            smapiLog.LogScoped(ScopedLogCatalog.ModWeCategory.Content, $"Failed to load Data/FarmAnimals for GMCM Animal Progression page: {ex}", LogLevel.Error);
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
        _contentDataLoader = new ModDataLoader(new SmapiJsonFileReader(Helper), _contentLog);

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
        var dataLoader = _contentDataLoader ?? new ModDataLoader(new SmapiJsonFileReader(Helper), log);
        var i18n = new I18TranslationHelper(Helper);

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

        var dataLoader = new ModDataLoader(new SmapiJsonFileReader(helper), log);
        var spawnWeather = new EggsInWild.Application.Spawning.SpawnWeatherHistory(saveData, log);
        var conditions = new EggsInWild.Application.Spawning.DefaultAreaConditionsEvaluator(game, spawnWeather);
        var planner = new EggsInWild.Application.Spawning.SpawnPlanner(
            world,
            new EggsInWild.Infrastructure.Mods.SmapiModAvailability(helper.ModRegistry),
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
            histogram: histogram,
            verifier: verifier);

        var dispatcher = new WeggCommandDispatcher(facade, i18n);
        var commands = new WeggConsoleCommands(log, i18n, dispatcher);
        commands.Register(helper);
    }

    private static void EnsureNoLocalNestCoreDuplicates(IModHelper helper)
    {
        var localNestCore = System.IO.Path.Combine(helper.DirectoryPath, "NestCore.dll");
        if (System.IO.File.Exists(localNestCore))
            throw new InvalidOperationException("Local NestCore.dll detected under EggsInWild. Remove it and rely on the insNestCoreed NestCore mod.");
    }
}
