using System;
using HarmonyLib;
using Mikew.BirdsOfAFeather.Framework;
using Mikew.BirdsOfAFeather.Helpers;
using WildEggsHatching.Application.DesertTomb.Generation;
using WildEggsHatching.Application.DesertTomb.Runtime;
using WildEggsHatching.Application.DesertTomb.Spawning;
using WildEggsHatching.DesertTomb.Core.Configuration;
using WildEggsHatching.Infrastructure.DesertTomb.Monsters;
using WildEggsHatching.Infrastructure.DesertTomb.Loot;
using WildEggsHatching.Infrastructure.DesertTomb.Loot.Definitions;
using WildEggsHatching.Infrastructure.DesertTomb.Diagnostics;
using WildEggsHatching.Infrastructure.DesertTomb.Harmony;
using WildEggsHatching.Infrastructure.Harmony;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using WildEggsHatching.Application.Facade;
using WildEggsHatching.Application.AnimalNotes;
using WildEggsHatching.Application.AnimalNotes.Messages;
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
using WildEggsHatching.Infrastructure.Diagnostics;
using WildEggsHatching.Infrastructure.Game;
using WildEggsHatching.Infrastructure.Logging;
using WildEggsHatching.Infrastructure.ModData;
using WildEggsHatching.Infrastructure.Mods;
using WildEggsHatching.Infrastructure.SaveData;
using WildEggsHatching.Infrastructure.Smapi;

namespace WildEggsHatching;

public sealed class ModEntry : Mod
{
    private CoreContext? _core;
    private IConfigProvider? _config;
    private ILog? _log;
    private IModFacade? _facade;
    private object? _api;
    private WeggConsoleCommands? _weggCommands;
    private SmapiContentCoordinator? _content;
    private BoafAssetRegistry? _assets;
    private ISaveDataStore? _saveData;
    private IModDataLoader? _dataLoader;

    private IGameStateReader? _game;
    private IWorldLocations? _world;
    private WildEggSpawnOrchestrator? _spawner;

    private ProgressionService? _progression;
    private int _tickCounter;

    private DesertTombRuntime? _desertTomb;

    private AnimalNotesOrchestrator? _animalNotes;
    private AnimalNotesClientObserver? _animalNotesClient;
    private AnimalNotesStartupValidator? _animalNotesValidator;
    private bool _pendingAnimalNotesValidation;

    private DesertTombDiagnostics? _desertTombDiagnostics;

    public override void Entry(IModHelper helper)
    {
        if (!helper.ModRegistry.IsLoaded("Mikew.birdsofafeather"))
        {
            Monitor.Log("Missing required dependency: BirdsOfAFeather (UniqueID: Mikew.birdsofafeather). Install it and try again.", LogLevel.Error);
            throw new InvalidOperationException("BirdsOfAFeather (Mikew.birdsofafeather) is required.");

        if (!helper.ModRegistry.IsLoaded("Pathoschild.ContentPatcher"))
        {
            Monitor.Log("Missing required dependency: Content Patcher (UniqueID: Pathoschild.ContentPatcher).", LogLevel.Error);
            throw new InvalidOperationException("Content Patcher (Pathoschild.ContentPatcher) is required.");
        }

        if (!helper.ModRegistry.IsLoaded("yourdorkbrains.cp.wildeggsforage"))
        {
            Monitor.Log("Missing required dependency: wildeggsforage content pack (UniqueID: yourdorkbrains.cp.wildeggsforage).", LogLevel.Error);
            throw new InvalidOperationException("yourdorkbrains.cp.wildeggsforage is required.");
        }
        }


        // Guardrail: BirdsOfAFeather must be installed as its own mod, not copied into this folder.
        try
        {
            var localDll = System.IO.Path.Combine(helper.DirectoryPath, "BirdsOfAFeather.dll");
            var localFolder = System.IO.Path.Combine(helper.DirectoryPath, "BirdsOfAFeather");
            if (System.IO.File.Exists(localDll) || System.IO.Directory.Exists(localFolder))
            {
                Monitor.Log("Local BirdsOfAFeather files detected inside WildEggsHatching folder. Remove them; BirdsOfAFeather must be installed as its own mod.", LogLevel.Error);
                throw new InvalidOperationException("Local BirdsOfAFeather duplicate detected.");
            }
        }
        catch
        {
            // best-effort; dependency check above already enforced
        }

        var configProvider = new SmapiConfigProvider(helper);
        _config = configProvider;

        var i18n = new I18TranslationHelper(helper);
        _core = new CoreContext(helper, Monitor, ModManifest, i18n);

        var boafLog = new BoafLog(Monitor, configProvider.Config);
        _log = boafLog;

        var log = (ILog)boafLog;
        var coreLogger = boafLog.Inner;

        _saveData = new SmapiSaveDataStore(helper);

        _assets = new BoafAssetRegistry(helper, Monitor, Constants.ModId);
        _assets.Register();
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
            ctx: _core!,
            config: configProvider,
            log: coreLogger,
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
        var conditions = new DefaultAreaConditionsEvaluator(_game);

        var planner = new SpawnPlanner(_world, mods, conditions, log);
        var placer = new GameObjectPlacer();
        var tileFinder = new GameTileFinder();
        var histogram = new TileSearchHistogram();
        var spawnService = new EggSpawnService(_world, placer, tileFinder, log, histogram);

        _spawner = _dataLoader is null
            ? null
            : new WildEggSpawnOrchestrator(_dataLoader, _game, planner, spawnService, log);

        var items = new GameItemRegistry();


        _animalNotes = !Context.IsMainPlayer || _dataLoader is null || _saveData is null || _progression is null
            ? null
            : new AnimalNotesOrchestrator(
                store: _saveData,
                dataLoader: _dataLoader,
                game: _game,
                i18n: i18n,
                debugMode: configProvider.Config.AnimalNotesDebugMode,
                warnAfterDays: configProvider.Config.AnimalNotesWarnAfterDays,
                placementRadius: configProvider.Config.AnimalNotesPlacementRadius,
                mail: mail,
                quests: quests,
                placer: placer,
                progression: _progression,
                log: log,
                getStep: () => _progression?.GetCurrentStep() ?? ProgressionStep.PartI,
                isMainPlayer: () => Context.IsMainPlayer,
                sendQueueMailMessage: (playerId, msg) =>
                {
                    try
                    {
                        Helper.Multiplayer.SendMessage(
                            message: msg,
                            messageType: "WeggAnimalNoteQueueMail",
                            modIDs: new[] { ModManifest.UniqueID },
                            playerIDs: new[] { playerId });
                    }
                    catch
                    {
                        // best-effort
                    }
                }
            );

        _animalNotesClient = _dataLoader is null
            ? null
            : new AnimalNotesClientObserver(
                helper: helper,
                log: log,
                dataLoader: _dataLoader,
                modUniqueId: ModManifest.UniqueID,
                getStep: () => _progression?.GetCurrentStep() ?? ProgressionStep.PartI,
                allowClientDiscoveries: () => configProvider.Config.AnimalNotesAllowClientDiscoveries
            );

        _animalNotesValidator = _dataLoader is null
            ? null
            : new AnimalNotesStartupValidator(
                dataLoader: _dataLoader,
                log: log,
                getStep: () => _progression?.GetCurrentStep() ?? ProgressionStep.PartI
            );




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
        var harmony = new Harmony(ModManifest.UniqueID + ".deserttomb");
        DesertTombMineShaftPatches.Install(harmony, _desertTomb);

        var harmonyNotes = new Harmony(ModManifest.UniqueID + ".animalnotes");
        AnimalNotesItemPatches.Install(harmonyNotes);



        var verifier = new WeggVerifier(helper, _saveData!, configProvider, _dataLoader!, log);

        _facade = new ModFacade(
            store: _saveData!,
            config: configProvider,
            log: log,
            isWorldReady: () => Context.IsWorldReady,
            isMainPlayer: () => Context.IsMainPlayer,
            reloadAll: () => ReloadAll(log),
            progression: _progression,
            spawner: _spawner,
            animalNotes: _animalNotes,
            animalNotesValidator: _animalNotesValidator,
            world: _world,
            items: items,
            game: _game,
            tileFinder: tileFinder,
            placer: placer,
            histogram: histogram,
            verifier: verifier
        );

        _weggCommands = new WeggConsoleCommands(Monitor, new WeggCommandDispatcher(_facade));
        _weggCommands.Register(helper);


        helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        helper.Events.GameLoop.Saving += OnSaving;
        helper.Events.Multiplayer.ModMessageReceived += OnModMessageReceived;
        helper.Events.Multiplayer.PeerConnected += OnPeerConnected;
        helper.Events.GameLoop.DayStarted += OnDayStarted;
        helper.Events.GameLoop.DayEnding += OnDayEnding;
        helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        helper.Events.Player.Warped += OnWarped;
        helper.Events.Display.RenderingHud += OnRenderingHud;
        configProvider.ConfigChanged += OnConfigChanged;
        _log?.Log("WildEggsHatching refactor Phase 3 loaded.", LogLevel.Info);
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

        if (Context.IsMainPlayer && _pendingAnimalNotesValidation)
        {
            _pendingAnimalNotesValidation = false;
            _animalNotesValidator?.ValidateAndLog();
        }
        _spawner?.OnDayStarted();
        _desertTomb?.OnDayStarted();
        _animalNotes?.OnDayStarted();
        _animalNotesClient?.OnDayStarted();
    }

    private void OnDayEnding(object? sender, DayEndingEventArgs e)
    {
        _spawner?.OnDayEnding();
        _progression?.OnDayEnding();
        _animalNotes?.OnDayEnding();
    }

    private void OnWarped(object? sender, WarpedEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;

        _desertTomb?.OnWarped(e.NewLocation);
        _animalNotes?.OnWarped(e.NewLocation);
        _animalNotesClient?.OnWarped(e.NewLocation);
    }
    private void OnModMessageReceived(object? sender, ModMessageReceivedEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;

        if (!string.Equals(e.FromModID, ModManifest.UniqueID, StringComparison.OrdinalIgnoreCase))
            return;

        // Client: accept host-granted quests.
        if (!Context.IsMainPlayer && string.Equals(e.Type, "WeggAnimalNoteGrantQuest", StringComparison.OrdinalIgnoreCase))
        {
            var msg = e.ReadAs<AnimalNoteGrantQuestMessage>();
            _animalNotesClient?.OnHostGrantQuest(msg);
            return;
        }

        // Client: accept host-queued mail.
        if (!Context.IsMainPlayer && string.Equals(e.Type, "WeggAnimalNoteQueueMail", StringComparison.OrdinalIgnoreCase))
        {
            var msg = e.ReadAs<AnimalNoteQueueMailMessage>();
            _animalNotesClient?.OnHostQueueMail(msg);
            return;
        }

        // Host only beyond this point.
        if (!Context.IsMainPlayer || _animalNotes is null)
            return;

        if (_config?.Config.AnimalNotesAllowClientDiscoveries != true)
            return;

        // Host: accept client discoveries.
        if (string.Equals(e.Type, "WeggAnimalNoteFound", StringComparison.OrdinalIgnoreCase))
        {
            var found = e.ReadAs<AnimalNoteFoundMessage>();

            if (!_animalNotes.TryHandleClientNoteFound(found, e.FromPlayerID, out var questId, out var shouldGrantQuest))
                return;

            if (!shouldGrantQuest || string.IsNullOrWhiteSpace(questId))
                return;

            try
            {
                // Tell the discovering client to add the quest locally (quests are per-farmer in MP).
                Helper.Multiplayer.SendMessage(
                    message: new AnimalNoteGrantQuestMessage { QuestId = questId, NoteItemId = found.NoteItemId },
                    messageType: "WeggAnimalNoteGrantQuest",
                    modIDs: new[] { ModManifest.UniqueID },
                    playerIDs: new[] { e.FromPlayerID });
            }
            catch
            {
                // best-effort
            }

            return;
        }

        // Host: accept client quest completion reports.
        if (string.Equals(e.Type, "WeggAnimalNoteQuestCompleted", StringComparison.OrdinalIgnoreCase))
        {
            var completed = e.ReadAs<AnimalNoteQuestCompletedMessage>();
            _animalNotes.TryHandleClientQuestCompleted(completed, e.FromPlayerID);
        }
    }



    
    private void OnPeerConnected(object? sender, PeerConnectedEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;

        if (!Context.IsMainPlayer || _animalNotes is null)
            return;

        if (_config?.Config.AnimalNotesAllowClientDiscoveries != true)
            return;

        try
        {
            var (quests, mail) = _animalNotes.GetPeerSyncPayload(e.Peer.PlayerID);

            foreach (var q in quests)
            {
                Helper.Multiplayer.SendMessage(
                    message: new AnimalNoteGrantQuestMessage { QuestId = q },
                    messageType: "WeggAnimalNoteGrantQuest",
                    modIDs: new[] { ModManifest.UniqueID },
                    playerIDs: new[] { e.Peer.PlayerID });
            }

            foreach (var m in mail)
            {
                Helper.Multiplayer.SendMessage(
                    message: new AnimalNoteQueueMailMessage { MailId = m, NoLetter = false },
                    messageType: "WeggAnimalNoteQueueMail",
                    modIDs: new[] { ModManifest.UniqueID },
                    playerIDs: new[] { e.Peer.PlayerID });
            }
        }
        catch
        {
            // best-effort
        }
    }

private void OnConfigChanged(object? sender, WildEggsHatching.Infrastructure.Config.ConfigChangedEventArgs e)
    {
        _log?.Log($"Config changed ({e.Reason}). Invalidating assets.", LogLevel.Info);
        _content?.InvalidateAll();
    }

    private void ReloadAll(ILog log)
    {
        if (_config is null || _dataLoader is null)
            return;

        _config.Reload();
        LoadModData(log);
        _content?.InvalidateAll();

        _log?.Log("Reloaded config + ModData.", LogLevel.Info);
    }

    private void LoadModData(ILog log)
    {
        if (_dataLoader is null)
            return;

        var snapshot = _dataLoader.Load();

        var modAvailability = new SmapiModAvailability(Helper.ModRegistry);
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
        _animalNotes?.OnSaveLoaded();
        _content?.InvalidateAll();

        try
        {
            if (_saveData is not null && _config is not null && _log is not null)
                Phase8HealthReporter.LogPhase8HealthReport(Helper, _saveData, _config, _log);
        }
        catch
        {
            // best-effort diagnostics
        }

        _pendingAnimalNotesValidation = true;
    }

    private void OnSaving(object? sender, SavingEventArgs e)
    {
        _animalNotes?.OnSaving();
        _progression?.OnDayEnding();
    }


    public override object? GetApi()
    {
        if (_facade is null)
            return null;

        return _api ??= new WildEggsApi(_facade);
    }
}