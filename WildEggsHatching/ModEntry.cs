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
using Mikew.BirdsOfAFeather.Helpers.Logging;
using WildEggsHatching.Infrastructure.ModData;
using WildEggsHatching.Infrastructure.Mods;
using Mikew.BirdsOfAFeather.Helpers.SaveData;
using Mikew.BirdsOfAFeather.Helpers.Config;

namespace WildEggsHatching;

public sealed class ModEntry : Mod
{
    private CoreContext? _core;
    private SmapiConfigProvider<ModConfig>? _config;
    private ILog? _log;
    private IModFacade? _facade;
    private object? _api;
    private WeggConsoleCommands? _weggCommands;
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
        FailFastDependencies(helper);
        EnsureNoLocalBoafDuplicates(helper);

        var i18n = new I18TranslationHelper(helper);
        _core = new CoreContext(helper, Monitor, ModManifest, i18n);

        _config = new SmapiConfigProvider<ModConfig>(Helper, Monitor);
        _config.ConfigUpdated += (_, __) => _assetPipeline?.RequestInvalidateAll();

        _log = new SmapiLog(Monitor);

        _saveData = new SmapiSaveDataStore(Helper, Monitor);

        _modAssets = new ModAssetHandler(Constants.ModId, Monitor);
        RegisterModAssets(_modAssets);
        _modAssets.SetUpAssets();

        _assetPipeline = new WeggAssetPipeline(
            helper: Helper,
            monitor: Monitor,
            contextFactory: () => new ContentContext(
                helper: Helper,
                monitor: Monitor,
                i18n: Helper.Translation,
                config: _config.Get(),
                step: _progression?.GetCurrentStep() ?? ProgressionStep.PartI
            )
        );

        _assetPipeline.RegisterModAssets(_modAssets);
        RegisterEditors(_assetPipeline);
        _assetPipeline.Attach();

        var modAssetPaths = new[]
        {
            "LooseSprites/Cursors",
            $"Mods/{Constants.ModId}/objects/note",
            $"Mods/{Constants.ModId}/animals/note",
            $"Mods/{Constants.ModId}/hats/birdhat",
            $"Mods/{Constants.ModId}/hats/cowboyhat",
        };

        var editorAssets = new[]
        {
            "Data/Objects",
            "Data/FarmAnimals",
            "Data/Mail",
            "Data/Quests",
            "Data/SpecialOrders",
            "Data/BigCraftables",
            "Data/CraftingRecipes",
            "Data/Machines",
            "Data/AdditionalMineMaps",
        };

        BoafPipelineSnapshot.Set(attached: true, editorAssets: editorAssets, modAssetPaths: modAssetPaths);


        var jsonReader = new SmapiJsonFileReader(helper);
        _dataLoader = new ModDataLoader(jsonReader, _log);

        LoadModData(_log);

        _game = new GameStateReader();
        _world = new WorldLocations();

        var mail = new GameMailService();
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

        RegisterCommands(helper, _log);

        helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        helper.Events.GameLoop.Saving += OnSaving;
        helper.Events.GameLoop.DayStarted += OnDayStarted;
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