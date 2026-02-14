using System;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using WildEggsHatching.Application.Progression;
using WildEggsHatching.Application.Spawning;
using WildEggsHatching.Core.Enums;
using WildEggsHatching.Infrastructure.Content;
using WildEggsHatching.Infrastructure.Content.Editors;
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
    private SmapiContentCoordinator? _content;
    private ISaveDataStore? _saveData;
    private IModDataLoader? _dataLoader;

    private IGameStateReader? _game;
    private IWorldLocations? _world;
    private WildEggSpawnOrchestrator? _spawner;

    private ProgressionStep _step = ProgressionStep.PartI;

    public override void Entry(IModHelper helper)
    {
        var configProvider = new SmapiConfigProvider(helper);

        _ctx = new ModContext(helper, Monitor, ModManifest, helper.Translation, configProvider);
        _saveData = new SmapiSaveDataStore(helper);

        var log = new SmapiLog(Monitor);
        _dataLoader = new ModDataLoader(new SmapiJsonFileReader(helper), log);

        LoadModData(log);

        _content = new SmapiContentCoordinator(
            ctx: _ctx,
            getStep: () => _step,
            registrations: new[]
            {
                new SmapiContentCoordinator.Registration(new ObjectsEditor(), ProgressionStep.PartI),
                new SmapiContentCoordinator.Registration(new FarmAnimalsEditor(), ProgressionStep.PartI),
                new SmapiContentCoordinator.Registration(new MailEditor(), ProgressionStep.PartII),
                new SmapiContentCoordinator.Registration(new QuestEditor(), ProgressionStep.PartI),
                new SmapiContentCoordinator.Registration(new SpecialOrdersEditor(), ProgressionStep.PartI),
                new SmapiContentCoordinator.Registration(new BigCraftablesEditor(), ProgressionStep.PartV),
                new SmapiContentCoordinator.Registration(new CraftingRecipesEditor(), ProgressionStep.PartV),
                new SmapiContentCoordinator.Registration(new MachinesEditor(), ProgressionStep.PartI),
            }
        );

        _content.Register();

        _game = new GameStateReader();
        _world = new WorldLocations();

        var mods = new SmapiModAvailability(helper.ModRegistry);
        var gsq = new SmapiGameStateQueryEvaluator();
        var conditions = new DefaultAreaConditionsEvaluator(_game, gsq);

        var planner = new SpawnPlanner(_world, mods, conditions, log);
        var placer = new GameObjectPlacer();
        var tileFinder = new GameTileFinder();
        var spawnService = new EggSpawnService(_world, placer, tileFinder, log);

        _spawner = _dataLoader is null ? null : new WildEggSpawnOrchestrator(_dataLoader, _game, planner, spawnService, log);

        helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        helper.Events.GameLoop.Saving += OnSaving;
        helper.Events.GameLoop.DayStarted += OnDayStarted;

        configProvider.ConfigChanged += OnConfigChanged;

        helper.ConsoleCommands.Add(
            name: "wildeggs_reload_all",
            documentation: "Reload config + ModData JSON (Phase 1).",
            callback: (_, _) => ReloadAll(log));

        _ctx.Log("WildEggsHatching refactor Phase 2 loaded.", LogLevel.Info);
    }

    private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        _spawner?.OnDayStarted();
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
        foreach (var _ in catalog.All()) count++;

        log.Log($"Loaded ModData: {snapshot.WildAnimals.Count} wild-animal entries, {snapshot.SpawnAreas.Count} spawn-area sets, {snapshot.LocationPacks.Count} location packs. Catalog={count}.",
            LogLevel.Debug);
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        if (_ctx is null || _content is null || _saveData is null)
            return;

        var env = _saveData.ReadEnvelope<ProgressionSaveData>(SaveKeys.Progression);
        if (env is null)
        {
            _step = ProgressionStep.PartI;
        }
        else if (env.Version != 1)
        {
            _ctx.Log($"Unsupported save data version {env.Version}; defaulting progression to PartI.", LogLevel.Warn);
            _step = ProgressionStep.PartI;
        }
        else
        {
            _step = env.Data.Step;
        }

        _content.InvalidateAll();
        _ctx.Log($"Loaded progression step: {_step}", LogLevel.Debug);
    }

    private void OnSaving(object? sender, SavingEventArgs e)
    {
        if (_saveData is null)
            return;

        _saveData.WriteEnvelope(SaveKeys.Progression, new SaveDataEnvelope<ProgressionSaveData>(1, new ProgressionSaveData(_step)));
    }
}
