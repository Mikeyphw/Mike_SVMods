using System;
using System.Linq;
using StardewValley;
using StardewValley.GameData.FarmAnimals;
using Mikew.BirdsOfAFeather.EnumsAndStrings;
using WildEggsHatching.Core.ShopAnchors;
using HarmonyLib;
using Mikew.BirdsOfAFeather.Framework;
using Mikew.BirdsOfAFeather;
using Mikew.BirdsOfAFeather.Helpers;
using WildEggsHatching.Application.DesertTomb.Generation;
using WildEggsHatching.Application.DesertTomb.Runtime;
using WildEggsHatching.Application.DesertTomb.Spawning;
using WildEggsHatching.DesertTomb.Core.Configuration;
using WildEggsHatching.Infrastructure.DesertTomb.Monsters;
using WildEggsHatching.Infrastructure.DesertTomb.Monsters.Definitions;
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
using WildEggsHatching.Application.Animals;
using WildEggsHatching.Core.Enums;
using WildEggsHatching.Infrastructure.Content;
using WildEggsHatching.Infrastructure.Content.Editors;
using WildEggsHatching.Infrastructure.Api;
using WildEggsHatching.Infrastructure.Assets;
using WildEggsHatching.Infrastructure.Commands;
using WildEggsHatching.Infrastructure.Diagnostics;
using WildEggsHatching.Infrastructure.Game;
using Mikew.BirdsOfAFeather.Helpers.Logging;
using WildEggsHatching.Infrastructure.ModData;
using WildEggsHatching.Infrastructure.Mods;
using Mikew.BirdsOfAFeather.Helpers.SaveData;
using Mikew.BirdsOfAFeather.Helpers.Config;
using System.IO;
using Mikew.BirdsOfAFeather.Models;
using System.Collections.Generic;
using WildEggsHatching.Core.Assets;
using WildEggsHatching.Core.ShopProgression;
using Mikew.BirdsOfAFeather.Integrations.Gmcm;
using WildEggsHatching.Infrastructure.Config;

namespace WildEggsHatching;

public sealed class ModEntry : Mod
{
    private CoreContext? _core;
    private SmapiConfigProvider<ModConfig>? _config;
    private GmcmConfigHost<ModConfig>? _gmcmHost;
    private ILog? _log;
    private IModFacade? _facade;
    private object? _api;
    private WeggConsoleCommands? _weggCommands;
            private ISaveDataStore? _saveData;
    private IModDataLoader? _dataLoader;
    private ModDataSnapshot? _modDataSnapshot;
    private ChosenEggAssignments? _chosenEggs;


    private ModAssetHandler? _modAssets;
    private WeggAssetPipeline? _assetPipeline;

    private IGameStateReader? _game;
    private IWorldLocations? _world;
    private WildEggSpawnOrchestrator? _spawner;
    private SpawnWeatherHistory? _spawnWeatherHistory;

    private ProgressionService? _progression;

    private const int ProgressionPollIntervalTicks = 600; // ~10s at 60 ticks/sec.
    private int _tickCounter;

    private DesertTombRuntime? _desertTomb;

    private AnimalNotesOrchestrator? _animalNotes;
    private AnimalNotesClientObserver? _animalNotesClient;
    private AnimalNotesStartupValidator? _animalNotesValidator;
    private bool _pendingAnimalNotesValidation;

    private bool _pendingInvalidateAllOnNextTick;
    private bool _pipelineAttached;

    private DesertTombDiagnostics? _desertTombDiagnostics;

    private PharaohTrackerOverlay? _pharaohTrackerOverlay;
    private Harmony? _harmony;

    public override void Entry(IModHelper helper)
    {
        FailFastDependencies(helper);
        EnsureNoLocalBoafDuplicates(helper);

        var i18n = new I18TranslationHelper(helper);
        _core = new CoreContext(helper, Monitor, ModManifest, i18n);

        _log = new SmapiLog(Monitor);

        _config = new SmapiConfigProvider<ModConfig>(Helper, _log);
        _config.ConfigUpdated += (_, __) => _assetPipeline?.RequestInvalidateAll();

        _saveData = new SmapiSaveDataStore(Helper, _log);
        var jsonReader = new SmapiJsonFileReader(helper);
        _dataLoader = new ModDataLoader(jsonReader, _log);

        var mail = new GameMailService();
        var unlocked = new AnimalUnlockedState(_saveData, mail);
        _chosenEggs = new ChosenEggAssignments(_dataLoader, helper.ModRegistry, _log, isUnlocked: unlocked.IsUnlocked);

        _modAssets = new ModAssetHandler(Constants.ModId, _log);
        RegisterModAssets(_modAssets);
        _modAssets.SetUpAssets();

        _assetPipeline = new WeggAssetPipeline(
            helper: Helper,
            monitor: Monitor,
            contextFactory: () => new ContentContext(
                helper: Helper,
                log: _log ?? new SmapiLog(Monitor),
                i18n: Helper.Translation,
                config: _config.Get(),
                step: _progression?.GetCurrentStep() ?? ProgressionStep.PartI,
                modData: _modDataSnapshot ?? ModDataSnapshot.Empty
            )
        );

        _assetPipeline.RegisterModAssets(_modAssets);
        RegisterEditors(_assetPipeline);


        var modAssetPaths = new[]
        {
            Strings.AssetNames.Cursors,
            $"Mods/{Constants.ModId}/objects/note",
            $"Mods/{Constants.ModId}/animals/note",
            $"Mods/{Constants.ModId}/hats/birdhat",
            $"Mods/{Constants.ModId}/hats/cowboyhat",
            $"Mods/{Constants.ModId}/misc/dinosaurShopIcon",
            $"Mods/{Constants.ModId}/misc/voidChickenShopIcon",
            $"Mods/{Constants.ModId}/misc/ostrichShopIcon",
        };

        var editorAssets = new[]
        {
            Strings.AssetNames.Objects,
            Strings.AssetNames.FarmAnimals,
            Strings.AssetNames.Mail,
            Strings.AssetNames.Quests,
            Strings.AssetNames.SpecialOrders,
            Strings.AssetNames.BigCraftables,
            Strings.AssetNames.CraftingRecipes,
            Strings.AssetNames.Machines,
        };

        BoafPipelineSnapshot.Set(attached: false, editorAssets: editorAssets, modAssetPaths: modAssetPaths);


        _game = new GameStateReader();
        _world = new WorldLocations();

        var quests = new GameQuestService();
        var recipes = new GameRecipeService();
        var facts = new GameUnlockFactsProvider(_game, mail, quests);

        _progression = new ProgressionService(
            store: _saveData,
            config: _config,
            facts: facts,
            quests: quests,
            mail: mail,
            recipes: recipes,
            log: _log,
            invalidateAssets: () => _assetPipeline?.RequestInvalidateAll()
        );

        
        // Runtime modules (wired via SMAPI events below).
        var getStep = new Func<ProgressionStep>(() => _progression?.GetCurrentStep() ?? ProgressionStep.PartI);

        var tileFinder = new GameTileFinder();
        var placer = new GameObjectPlacer();
        var histogram = new TileSearchHistogram();

        var mods = new SmapiModAvailability(helper.ModRegistry);
        _spawnWeatherHistory = new SpawnWeatherHistory(_saveData!, _log!);
        var conditions = new DefaultAreaConditionsEvaluator(_game, _spawnWeatherHistory);

        var planner = new SpawnPlanner(_world, mods, conditions, _log);
        var spawnService = new EggSpawnService(_world, placer, tileFinder, _log, histogram);
        _spawner = new WildEggSpawnOrchestrator(_dataLoader, _game, planner, spawnService, _log);

        _animalNotesValidator = new AnimalNotesStartupValidator(_dataLoader, _log, getStep);

        Action<long, AnimalNoteQueueMailMessage> sendQueueMailMessage = (playerId, msg) =>
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
        };

        var cfg = _config.Get();
        _animalNotes = new AnimalNotesOrchestrator(
            store: _saveData,
            dataLoader: _dataLoader,
            game: _game,
            i18n: i18n,
            debugMode: cfg.AnimalNotesDebugMode,
            warnAfterDays: cfg.AnimalNotesWarnAfterDays,
            placementRadius: cfg.AnimalNotesPlacementRadius,
            mail: mail,
            quests: quests,
            placer: placer,
            progression: _progression,
            log: _log,
            getStep: getStep,
            isMainPlayer: () => Context.IsMainPlayer,
            sendQueueMailMessage: sendQueueMailMessage
        );

        _animalNotesClient = new AnimalNotesClientObserver(
            helper: helper,
            log: _log,
            dataLoader: _dataLoader,
            modUniqueId: ModManifest.UniqueID,
            getStep: getStep,
            allowClientDiscoveries: () => _config.Get().AnimalNotesAllowClientDiscoveries
        );

        if (cfg.DesertTombModuleEnabled)
        {
            _desertTombDiagnostics = new DesertTombDiagnostics(helper, _config, _log);
            _pharaohTrackerOverlay = new PharaohTrackerOverlay(helper, _config);

            var spawnConfig = new SpawnConfiguration();
            var generator = new DesertTombLevelGenerator(spawnConfig);

            var monsterDefinitions = new TombMonsterDefinitions(jsonReader, _log);
            var dropTables = new TombDropTables(jsonReader, _log);
            var dropEngine = new TombDropTableEngine();
            var monsterFactory = new DesertTombMonsterFactory(monsterDefinitions, dropTables, dropEngine, _log);

            var spawnExecutor = new DesertTombSpawnExecutor(monsterFactory, _log);

            _desertTomb = new DesertTombRuntime(
                config: _config,
                getStep: getStep,
                log: _log,
                isMainPlayer: () => Context.IsMainPlayer,
                generator: generator,
                spawnExecutor: spawnExecutor,
                diagnostics: _desertTombDiagnostics
            );
        }


        InstallHarmonyPatches();

        RegisterCommands(helper, _log);

        helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
        helper.Events.GameLoop.Saving += OnSaving;
        helper.Events.GameLoop.DayStarted += OnDayStarted;
        helper.Events.GameLoop.TimeChanged += OnTimeChanged;
        helper.Events.GameLoop.DayEnding += OnDayEnding;
        helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        helper.Events.Player.Warped += OnWarped;
        helper.Events.Multiplayer.ModMessageReceived += OnModMessageReceived;
        helper.Events.Multiplayer.PeerConnected += OnPeerConnected;
        helper.Events.Display.RenderingHud += OnRenderingHud;
    }



    private static void FailFastDependencies(IModHelper helper)
    {
        const string boafId = "Mikew.birdsofafeather";

        // SMAPI enforces required deps from manifest, but we fail-fast too for clarity.
        if (!helper.ModRegistry.IsLoaded(boafId))
            throw new InvalidOperationException($"Missing required dependency '{boafId}'.");
    }

    private static void EnsureNoLocalBoafDuplicates(IModHelper helper)
    {
        // Avoid bundling BOAF inside this mod (type identity issues + violates BOAF rules).
        var localBoaf = Path.Combine(helper.DirectoryPath, "BirdsOfAFeather.dll");
        if (File.Exists(localBoaf))
            throw new InvalidOperationException("Local BirdsOfAFeather.dll detected under WildEggsHatching. Remove it and rely on the installed BOAF mod.");
    }

    private static void RegisterModAssets(ModAssetHandler handler)
    {
        handler.AddAssets(new List<AssetData>
        {
            // Mods/<modId>/<folder>/<asset>
            new(typeof(WeggObjectTexture), WeggAssetFolder.objects, ModAssetHandler.FileExtension.Png, $"Mods/{Constants.ModId}"),
            new(typeof(WeggBigCraftableTexture), WeggAssetFolder.bigcraftables, ModAssetHandler.FileExtension.Png, $"Mods/{Constants.ModId}"),
            new(typeof(WeggMiscTexture), WeggAssetFolder.misc, ModAssetHandler.FileExtension.Png, $"Mods/{Constants.ModId}"),

            // Vanilla-like keys.
            new(typeof(WeggMineMap), WeggAssetFolder.mines, ModAssetHandler.FileExtension.Tmx, WeggGameAssetNames.MinesMapsRoot),
            new(typeof(WeggMineTexture), WeggAssetFolder.mines, ModAssetHandler.FileExtension.Png, WeggGameAssetNames.MinesMapsRoot),
            new(typeof(WeggMonsterTexture), WeggAssetFolder.mines, ModAssetHandler.FileExtension.Png, "Characters/Monsters"),
        });
    }


    private void EnsurePipelineAttached()
    {
        if (_pipelineAttached)
            return;

        if (_assetPipeline is null)
            return;

        _assetPipeline.Attach();

        var modAssetPaths = new[]
        {
            Strings.AssetNames.Cursors,
            $"Mods/{Constants.ModId}/objects/note",
            $"Mods/{Constants.ModId}/animals/note",
            $"Mods/{Constants.ModId}/hats/birdhat",
            $"Mods/{Constants.ModId}/hats/cowboyhat",
            $"Mods/{Constants.ModId}/misc/dinosaurShopIcon",
            $"Mods/{Constants.ModId}/misc/voidChickenShopIcon",
            $"Mods/{Constants.ModId}/misc/ostrichShopIcon",
        };

        var editorAssets = new[]
        {
            Strings.AssetNames.Objects,
            Strings.AssetNames.FarmAnimals,
            Strings.AssetNames.Mail,
            Strings.AssetNames.Quests,
            Strings.AssetNames.SpecialOrders,
            Strings.AssetNames.BigCraftables,
            Strings.AssetNames.CraftingRecipes,
            Strings.AssetNames.Machines,
        };

        BoafPipelineSnapshot.Set(attached: true, editorAssets: editorAssets, modAssetPaths: modAssetPaths);
        _pipelineAttached = true;
    }

    private void RegisterEditors(WeggAssetPipeline pipeline)
    {
        Func<ProgressionStep> getStep = () => _progression?.GetCurrentStep() ?? ProgressionStep.PartI;

        // Editors do additional step-gating internally; keep pipeline gating permissive.
        pipeline.RegisterEditor(new ObjectsEditor(), ProgressionStep.PartI, getStep, order: 0);
        pipeline.RegisterEditor(new WildEggIncubatorTextureEditor(), ProgressionStep.PartI, getStep, order: 5);
        pipeline.RegisterEditor(new FarmAnimalsEditor(_chosenEggs ?? throw new InvalidOperationException(\"ChosenEggAssignments not initialized.\")), ProgressionStep.PartI, getStep, order: 10);
        pipeline.RegisterEditor(new BigCraftablesEditor(), ProgressionStep.PartI, getStep, order: 20);
        pipeline.RegisterEditor(new CraftingRecipesEditor(), ProgressionStep.PartI, getStep, order: 30);
        pipeline.RegisterEditor(new MachinesEditor(), ProgressionStep.PartI, getStep, order: 40);
        pipeline.RegisterEditor(new MonstersEditor(), ProgressionStep.PartI, getStep, order: 45);
        pipeline.RegisterEditor(new MailEditor(), ProgressionStep.PartI, getStep, order: 50);
        pipeline.RegisterEditor(new QuestEditor(), ProgressionStep.PartI, getStep, order: 60);
        pipeline.RegisterEditor(new SpecialOrdersEditor(), ProgressionStep.PartI, getStep, order: 70);
    }

    private void RegisterCommands(IModHelper helper, ILog log)
    {
        if (_config is null || _saveData is null || _dataLoader is null || _world is null || _game is null)
            return;

        var verifier = new WeggVerifier(helper, _saveData, _config, _dataLoader, log);

        _facade = new ModFacade(
            store: _saveData,
            config: _config,
            log: log,
            isWorldReady: () => Context.IsWorldReady,
            isMainPlayer: () => Context.IsMainPlayer,
            reloadAll: () => ReloadAll(log),
            progression: _progression,
            spawner: _spawner,
            animalNotes: _animalNotes,
            animalNotesValidator: _animalNotesValidator,
            world: _world,
            items: new GameItemRegistry(),
            game: _game,
            tileFinder: new GameTileFinder(),
            placer: new GameObjectPlacer(),
            verifier: verifier
        );

        var dispatcher = new WeggCommandDispatcher(_facade);
        _weggCommands = new WeggConsoleCommands(_log ?? new SmapiLog(Monitor), dispatcher);
        _weggCommands.Register(helper);
    }

    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;

        if (_pendingInvalidateAllOnNextTick)
        {
            _pendingInvalidateAllOnNextTick = false;
            _assetPipeline?.RequestInvalidateAll();
        }

        _tickCounter++;
        _progression?.OnUpdateTicked(_tickCounter % ProgressionPollIntervalTicks == 0);
        _desertTomb?.OnUpdateTicked();
    }

    private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        _progression?.OnDayStarted();

        _spawnWeatherHistory?.UpdateForDayStarted(_game?.GetDayNumber() ?? 0);

        var step = _progression?.GetCurrentStep() ?? ProgressionStep.PartI;
        _chosenEggs?.RecomputeIfNeeded(step);
        if (step >= ProgressionStep.PartVI)
            _assetPipeline?.RequestInvalidate(Strings.AssetNames.FarmAnimals);

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


    private void OnTimeChanged(object? sender, TimeChangedEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;

        var step = _progression?.GetCurrentStep() ?? ProgressionStep.PartI;
        if (step < ProgressionStep.PartVI)
            return;

        // Force a refresh of chosen egg -> animal mapping for this time bucket.
        _chosenEggs?.Clear();
        _assetPipeline?.RequestInvalidate(Strings.AssetNames.FarmAnimals);
    }

    private void OnDayEnding(object? sender, DayEndingEventArgs e)
    {
        _chosenEggs?.Clear();
        _spawner?.OnDayEnding();
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

        if (_config?.Get().AnimalNotesAllowClientDiscoveries != true)
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

        if (_config?.Get().AnimalNotesAllowClientDiscoveries != true)
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
        _pendingInvalidateAllOnNextTick = true;
    }

    private void ReloadAll(ILog log)
    {
        if (_config is null || _dataLoader is null)
            return;

        _config.Reload();
        LoadModData(log);
        _pendingInvalidateAllOnNextTick = true;

        _log?.Log("Reloaded config + ModData.", LogLevel.Info);
    }

    private void LoadModData(ILog log)
    {
        if (_dataLoader is null)
            return;

        var snapshot = _dataLoader.Load();
        _modDataSnapshot = snapshot;

        SeedAnimalProgressionModeDefaults(snapshot);
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

    private void SeedAnimalProgressionModeDefaults(ModDataSnapshot snapshot)
    {
        try
        {
            if (_config is null || snapshot is null)
                return;

            var cfg = _config.Get();
            cfg.AnimalProgressionMode ??= new System.Collections.Generic.Dictionary<string, bool>(System.StringComparer.OrdinalIgnoreCase);

            var changed = false;
            foreach (var key in snapshot.WildAnimals.Keys)
            {
                if (string.IsNullOrWhiteSpace(key))
                    continue;

                if (!cfg.AnimalProgressionMode.ContainsKey(key))
                {
                    cfg.AnimalProgressionMode[key] = AnimalShopProgressionPolicy.DefaultEnabled(key);
                    changed = true;
                }
            }

            if (changed)
            {
                Helper.WriteConfig(cfg);
                _log?.Log($"Config: seeded AnimalProgressionMode defaults for {snapshot.WildAnimals.Count} animals.", LogLevel.Info);
            }
        }
        catch (Exception ex)
        {
            _log?.Log($"Config: failed seeding AnimalProgressionMode defaults. {ex.GetType().Name}: {ex.Message}", LogLevel.Trace);
        }
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

        if (_log is not null)
            LoadModData(_log);

        EnsurePipelineAttached();
    }

    private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
    {
        RebuildGmcmLayout(force: true);
    }

    private void RebuildGmcmLayout(bool force)
    {
        if (_gmcmHost is null || _config is null)
            return;

        _gmcmHost.RebuildLayout(layout =>
        {
            layout.Root(root =>
            {
                root.Bool(cfg => cfg.ProgressionMode, optionId: "general.progressionMode", tooltip: true);
                root.Bool(cfg => cfg.DesertTombModuleEnabled, optionId: "desertTomb.enabled", tooltip: true);

                root.Separator("pages", simple: true);
                root.Link("desertTomb");
                root.Link("items");
                root.Link("hatching");
                root.Link("prices");
                root.Link("animalNotes");
                root.Link("animalProgression");
                root.Link("debug");
            });

            layout.Page("desertTomb", page =>
            {
                page.Enum(cfg => cfg.TombChance, optionId: "desertTomb.tombChance", tooltip: true);
            });

            layout.Page("items", page =>
            {
                page.Bool(cfg => cfg.AlternatePotionSprite, optionId: "items.alternatePotionSprite", tooltip: true);
                page.Bool(cfg => cfg.NeedsFern, optionId: "items.incubatorNeedsFern", tooltip: true);
            });

            layout.Page("hatching", page =>
            {
                page.Bool(cfg => cfg.HatchDinoEggs, optionId: "hatching.hatchDinoEggs", tooltip: true);
                page.Bool(cfg => cfg.DinoEggsExclusiveHatch, optionId: "hatching.dinoExclusiveHatch", tooltip: true);
            });

            layout.Page("prices", page =>
            {
                page.Text(cfg => cfg.DinoPrice, optionId: "prices.dino", tooltip: true);
                page.Text(cfg => cfg.VoidChickenPrice, optionId: "prices.voidChicken", tooltip: true);
                page.Text(cfg => cfg.OstrichPrice, optionId: "prices.ostrich", tooltip: true);
            });

            layout.Page("animalNotes", page =>
            {
                page.Int(cfg => cfg.AnimalNotesWarnAfterDays, optionId: "animalNotes.warnAfterDays", tooltip: true, min: 0);
                page.Int(cfg => cfg.AnimalNotesPlacementRadius, optionId: "animalNotes.placementRadius", tooltip: true, min: 0);
                page.Bool(cfg => cfg.AnimalNotesQuestMailHostOnly, optionId: "animalNotes.hostOnlyQuestMail", tooltip: true);
                page.Bool(cfg => cfg.AnimalNotesAllowClientDiscoveries, optionId: "animalNotes.allowClientDiscoveries", tooltip: true);

                page.Separator("animalNotes_advanced", simple: true);
                page.Link("animalProgression", tooltip: true);
                page.Paragraph("animalNotes.progressionOverrides");
            });

            layout.Page("animalProgression", BuildAnimalProgressionPage);

            layout.Page("debug", page =>
            {
                page.ParagraphGmcmOptionsStatus();
                page.Separator("debug_status", simple: true);

                page.Bool(cfg => cfg.DesertTombPharaohTrackerEnabled, optionId: "debug.desertTomb.pharaohTracker", tooltip: true);
                page.Bool(cfg => cfg.DesertTombDebugLogging, optionId: "debug.desertTomb.traceLogging", tooltip: true);

                page.Separator("debug_animalNotes", simple: true);

                page.Bool(cfg => cfg.AnimalNotesDebugMode, optionId: "debug.animalNotes.debugMode", tooltip: true);
            });
        }, force: force);
    }

    private void BuildAnimalProgressionPage(GmcmPageBuilder<ModConfig> page)
    {
        page.Paragraph("animalProgression.description");

        if (!Context.IsWorldReady)
        {
            page.Separator("animalProgression_loadSave", simple: true);
            page.Paragraph("animalProgression.loadSave");
            return;
        }

        Dictionary<string, FarmAnimalData>? farmAnimals;
        try
        {
            farmAnimals = Game1.content.Load<Dictionary<string, FarmAnimalData>>(Strings.AssetNames.FarmAnimals);
        }
        catch
        {
            page.Separator("animalProgression_loadFailed", simple: true);
            page.Paragraph("animalProgression.loadFailed");
            return;
        }

        if (farmAnimals is null || farmAnimals.Count == 0)
        {
            page.Separator("animalProgression_empty", simple: true);
            page.Paragraph("animalProgression.empty");
            return;
        }

        var config = _config!.Get();
        var anchors = ShopAnchorIndex.Build(farmAnimals);
        var enabledText = Helper.Translation.Get("config.animalProgression.state.enabled").ToString();
        var disabledText = Helper.Translation.Get("config.animalProgression.state.disabled").ToString();

        page.Section("animalProgression.animals");

        foreach (var row in farmAnimals
                     .Where(kvp => !string.Equals(kvp.Key, "Blue Chicken", StringComparison.OrdinalIgnoreCase))
                     .Select(kvp => new
                     {
                         Id = kvp.Key,
                         Name = kvp.Value?.ShopDisplayName ?? kvp.Value?.DisplayName ?? kvp.Key
                     })
                     .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                     .ThenBy(x => x.Id, StringComparer.OrdinalIgnoreCase))
        {
            var animalId = row.Id;
            var displayName = row.Name;

            var defaultEnabled = AnimalShopProgressionPolicy.DefaultEnabled(animalId, farmAnimals, anchors);
            var defaultState = defaultEnabled ? enabledText : disabledText;

            var elementId = GmcmDynamicIds.ElementId("animalProgression", animalId);

            page.BoolLabel(
                elementId: elementId,
                name: () => displayName,
                tooltip: () => Helper.Translation.Get(
                        "config.animalProgression.entry.tooltip",
                        new { animalId, defaultState }
                    )
                    .ToString(),
                getValue: () =>
                {
                    var dict = config.AnimalProgressionMode;
                    if (dict is not null)
                    {
                        if (dict.TryGetValue(animalId, out var enabled))
                            return enabled;

                        var match = dict.Keys.FirstOrDefault(k => string.Equals(k, animalId, StringComparison.OrdinalIgnoreCase));
                        if (!string.IsNullOrWhiteSpace(match) && dict.TryGetValue(match, out enabled))
                            return enabled;
                    }

                    return defaultEnabled;
                },
                setValue: value =>
                {
                    EnsureAnimalProgressionComparer(config);

                    var dict = config.AnimalProgressionMode!;
                    if (value == defaultEnabled)
                        dict.Remove(animalId);
                    else
                        dict[animalId] = value;
                }
            );
        }
    }

    private static void EnsureAnimalProgressionComparer(ModConfig config)
    {
        if (config.AnimalProgressionMode is null)
        {
            config.AnimalProgressionMode = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            return;
        }

        if (config.AnimalProgressionMode.Comparer != StringComparer.OrdinalIgnoreCase)
        {
            config.AnimalProgressionMode =
                new Dictionary<string, bool>(config.AnimalProgressionMode, StringComparer.OrdinalIgnoreCase);
        }
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        _progression?.OnSaveLoaded();
        _animalNotes?.OnSaveLoaded();
        _pendingInvalidateAllOnNextTick = true;

        try
        {
            if (_saveData is not null && _config is not null && _log is not null)
                Phase8HealthReporter.LogPhase8HealthReport(Helper, _saveData, _config, _log);
        }
        catch
        {
            // best-effort diagnostics
        }

        RebuildGmcmLayout(force: true);

        _pendingAnimalNotesValidation = true;
    }

    private void OnSaving(object? sender, SavingEventArgs e)
    {
        _animalNotes?.OnSaving();
    }


    public override object? GetApi()
    {
        if (_facade is null)
            return null;

        return _api ??= new WildEggsApi(_facade);
    }

    private void InstallHarmonyPatches()
    {
        if (_harmony is not null)
            return;

        _harmony = new Harmony($"{Constants.ModId}.harmony");

        AnimalNotesItemPatches.Install(_harmony);

        if (_desertTomb is not null)
            DesertTombMineShaftPatches.Install(_harmony, _desertTomb);
    }

    private void OnRenderingHud(object? sender, RenderingHudEventArgs e)
    {
        _pharaohTrackerOverlay?.Render(e.SpriteBatch);
    }


}