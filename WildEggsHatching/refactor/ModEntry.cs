using System;
using WildEggsHatching.Application.DesertTomb.Generation;
using WildEggsHatching.Application.DesertTomb.Runtime;
using WildEggsHatching.Application.DesertTomb.Spawning;
using WildEggsHatching.DesertTomb.Core.Configuration;
using WildEggsHatching.Infrastructure.DesertTomb.Monsters;
using WildEggsHatching.Infrastructure.DesertTomb.Loot;
using WildEggsHatching.Infrastructure.DesertTomb.Loot.Definitions;
using WildEggsHatching.Infrastructure.DesertTomb.Diagnostics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using WildEggsHatching.Application.Facade;
using WildEggsHatching.Application.Progression;
using WildEggsHatching.Application.Spawning;
using WildEggsHatching.Application.Spawning.Diagnostics;
using WildEggsHatching.Core.Enums;
using WildEggsHatching.Infrastructure.Content;
using WildEggsHatching.Infrastructure.Content.Editors;
using WildEggsHatching.Infrastructure.Api;
using WildEggsHatching.Infrastructure.Assets;
using WildEggsHatching.Infrastructure.DesertTomb.Assets;
using WildEggsHatching.Infrastructure.Commands;
using WildEggsHatching.Infrastructure.Game;
using WildEggsHatching.Infrastructure.Logging;
using WildEggsHatching.Infrastructure.ModData;
using WildEggsHatching.Infrastructure.Mods;
using WildEggsHatching.Infrastructure.SaveData;
using WildEggsHatching.Infrastructure.Smapi;

namespace WildEggsHatching;

public sealed class ModEntry : Mod
{
    private ModContext? _ctx;
    private ILog? _log;
    private IModFacade? _facade;
    private object? _api;
    private WeggConsoleCommands? _weggCommands;
    private SmapiContentCoordinator? _content;
    private ModAssetLoader? _assets;
    private ISaveDataStore? _saveData;
    private IModDataLoader? _dataLoader;

    private IGameStateReader? _game;
    private IWorldLocations? _world;
    private WildEggSpawnOrchestrator? _spawner;

    private ProgressionService? _progression;
    private int _tickCounter;

    private DesertTombRuntime? _desertTomb;

    private DesertTombDiagnostics? _desertTombDiagnostics;

    public override void Entry(IModHelper helper)
    {
        var configProvider = new SmapiConfigProvider(helper);

        _ctx = new ModContext(helper, Monitor, ModManifest, helper.Translation, configProvider);
        _saveData = new SmapiSaveDataStore(helper);

        _assets = new ModAssetLoader(helper, Monitor);
        _assets.Register();

        _log = new SmapiLog(Monitor);

        var log = _log;
        var jsonReader = new SmapiJsonFileReader(helper);
        _dataLoader = new ModDataLoader(jsonReader, log);

        LoadModData(log);

        _game = new GameStateReader();
        _world = new WorldLocations();

        var mail = new GameMailService();
        var quests = new GameQuestService();
        var recipes = new GameRecipeService();
        var facts = new GameUnlockFactsProvider(_game, mail, quests);
        _progression = new ProgressionService(
            store: _saveData,
            config: configProvider,
            facts: facts,
            quests: quests,
            mail: mail,
            recipes: recipes,
            log: log,
            invalidateAssets: () => _content?.InvalidateAll()
        );

        _content = new SmapiContentCoordinator(
            ctx: _ctx,
            getStep: () => _progression?.GetCurrentStep() ?? ProgressionStep.PartI,
            registrations: new[]
            {
                new SmapiContentCoordinator.Registration(new ObjectsEditor(), ProgressionStep.PartI),
                new SmapiContentCoordinator.Registration(new FarmAnimalsEditor(), ProgressionStep.PartI),
                new SmapiContentCoordinator.Registration(new MailEditor(), ProgressionStep.PartII),
                new SmapiContentCoordinator.Registration(new QuestEditor(), ProgressionStep.PartI),
                new SmapiContentCoordinator.Registration(new SpecialOrdersEditor(), ProgressionStep.PartI),
                new SmapiContentCoordinator.Registration(new BigCraftablesEditor(), ProgressionStep.PartV),
                new SmapiContentCoordinator.Registration(new CraftingRecipesEditor(), ProgressionStep.PartV),
                new SmapiContentCoordinator.Registration(new MachinesEditor(), ProgressionStep.PartV),
                new SmapiContentCoordinator.Registration(new DesertTombAdditionalMineMapsEditor(), ProgressionStep.PartI),
            }
        );

        _content.Register();

        var mods = new SmapiModAvailability(helper.ModRegistry);
        var gsq = new SmapiGameStateQueryEvaluator();
        var conditions = new DefaultAreaConditionsEvaluator(_game, gsq);

        var planner = new SpawnPlanner(_world, mods, conditions, log);
        var placer = new GameObjectPlacer();
        var tileFinder = new GameTileFinder();
        var histogram = new TileSearchHistogram();
        var spawnService = new EggSpawnService(_world, placer, tileFinder, log, histogram);

        _spawner = _dataLoader is null
            ? null
            : new WildEggSpawnOrchestrator(_dataLoader, _game, planner, spawnService, log);

        var items = new GameItemRegistry();

        // DesertTomb runtime (Phase 0): activate tomb floors and spawn tomb monsters.
        var tombConfig = SpawnConfiguration.Default;
        var tombGenerator = new DesertTombLevelGenerator(tombConfig);
        var tombDefinitions = new WildEggsHatching.Infrastructure.DesertTomb.Monsters.Definitions.TombMonsterDefinitions(jsonReader, log);
        var tombDropTables = new TombDropTables(jsonReader, log);
        var tombDropEngine = new TombDropTableEngine();
        var tombFactory = new DesertTombMonsterFactory(tombDefinitions, tombDropTables, tombDropEngine, log);
        var tombSpawnExecutor = new DesertTombSpawnExecutor(tombFactory, log);
        _desertTombDiagnostics = new DesertTombDiagnostics(helper, configProvider, log);
        _desertTomb = new DesertTombRuntime(
            config: configProvider,
            getStep: () => _progression?.GetCurrentStep() ?? ProgressionStep.PartI,
            log: log,
            isMainPlayer: () => Context.IsMainPlayer,
            generator: tombGenerator,
            spawnExecutor: tombSpawnExecutor,
            diagnostics: _desertTombDiagnostics
        );


        _facade = new ModFacade(
            config: configProvider,
            log: log,
            isWorldReady: () => Context.IsWorldReady,
            isMainPlayer: () => Context.IsMainPlayer,
            reloadAll: () => ReloadAll(log),
            progression: _progression,
            spawner: _spawner,
            world: _world,
            items: items,
            game: _game,
            tileFinder: tileFinder,
            placer: placer,
            histogram: histogram
        );

        _weggCommands = new WeggConsoleCommands(Monitor, new WeggCommandDispatcher(_facade));
        _weggCommands.Register(helper);


        helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        helper.Events.GameLoop.Saving += OnSaving;
        helper.Events.GameLoop.DayStarted += OnDayStarted;
        helper.Events.GameLoop.DayEnding += OnDayEnding;
        helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        helper.Events.Player.Warped += OnWarped;
        helper.Events.Display.RenderingHud += OnRenderingHud;
        configProvider.ConfigChanged += OnConfigChanged;

        helper.ConsoleCommands.Add(
            name: "wildeggs_reload_all",
            documentation: "Reload config + ModData JSON (Phase 1).",
            callback: (_, _) => _facade?.ReloadAll()
        );

        _ctx.Log("WildEggsHatching refactor Phase 3 loaded.", LogLevel.Info);
    }

    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;

        _tickCounter++;
        _progression?.OnUpdateTicked(isMultipleOf60Ticks: _tickCounter % 60 == 0);
        _desertTomb?.OnUpdateTicked();
    }

    private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        _progression?.OnDayStarted();
        _spawner?.OnDayStarted();
        _desertTomb?.OnDayStarted();
    }

    private void OnDayEnding(object? sender, DayEndingEventArgs e)
    {
        _spawner?.OnDayEnding();
        _progression?.OnDayEnding();
    }

    private void OnWarped(object? sender, WarpedEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;

        _desertTomb?.OnWarped(e.NewLocation);
    }

    private void OnConfigChanged(object? sender, WildEggsHatching.Infrastructure.Config.ConfigChangedEventArgs e)
    {
        _ctx?.Log($"Config changed ({e.Reason}). Invalidating assets.", LogLevel.Info);
        _content?.InvalidateAll();
    }

    private void ReloadAll(ILog log)
    {
        if (_ctx is null || _dataLoader is null)
            return;

        _ctx.ConfigProvider.Reload();
        LoadModData(log);
        _content?.InvalidateAll();

        _ctx.Log("Reloaded config + ModData.", LogLevel.Info);
    }

    private void LoadModData(ILog log)
    {
        if (_dataLoader is null)
            return;

        var snapshot = _dataLoader.Load();

        var modAvailability = _ctx is not null
            ? new SmapiModAvailability(_ctx.Helper.ModRegistry)
            : new SmapiModAvailability(Helper.ModRegistry);

        var catalog = new AnimalCatalogBuilder(modAvailability, log).Build(snapshot.WildAnimals);

        var count = 0;
        foreach (var _ in catalog.All())
            count++;

        log.Log(
            $"Loaded ModData: {snapshot.WildAnimals.Count} wild-animal entries, {snapshot.SpawnAreas.Count} spawn-area sets, {snapshot.LocationPacks.Count} location packs. Catalog={count}.",
            LogLevel.Debug
        );
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        _progression?.OnSaveLoaded();
        _content?.InvalidateAll();
    }

    private void OnSaving(object? sender, SavingEventArgs e)
    {
        _progression?.OnDayEnding();
    }


    public override object? GetApi()
    {
        if (_facade is null)
            return null;

        return _api ??= new WildEggsApi(_facade);
    }
}