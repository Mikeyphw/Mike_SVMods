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
using StardewValley;
using StardewValley.GameData.FarmAnimals;
using EggsInWild.Application.Animals;
using EggsInWild.Application.Facade;
using EggsInWild.Application.Progression;
using EggsInWild.Core.Enums;
using EggsInWild.Core.ShopAnchors;
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
using EggsInWild.Infrastructure.DesertTomb.Diagnostics;
using EggsInWild.DesertTomb.Core.Configuration;
using EggsInWild.Infrastructure.ModData;
using EggsInWild.Infrastructure.ModData.Models;
using EggsInWild.Infrastructure.Assets;
using EggsInWild.Application.AnimalNotes;
using Mikew.NestCore.Framework;
using static Mikew.NestCore.EnumsAndStrings.Strings;
using StardewModdingAPI.Utilities;

namespace EggsInWild;

public sealed partial class ModEntry : Mod
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
    private ChosenEggAssignments? _chosenEggAssignments;

    // Bulk state moved to EiwGmcmContext
    // private readonly Dictionary<string, ExtraModAnimalKeysBulkState> _extraModAnimalKeysBulkByWildAnimalKey = new(StringComparer.OrdinalIgnoreCase);
    private HashSet<string> _knownLocationTypeKeysForNormalization = new(StringComparer.OrdinalIgnoreCase);

    private DesertTombRuntime? _desertTombRuntime;
    private Infrastructure.DesertTomb.Diagnostics.DesertTombDiagnostics? _desertTombDiagnostics;
    private bool _desertTombHarmonyInstalled;

    public override void Entry(IModHelper helper)
    {
        _config = new SmapiConfigProvider<ModConfig>(helper, Monitor);
        _config.ConfigUpdated += OnConfigUpdated;

        DebugKnobs.Initialize(() => _config.Get());
        CustomMonsterDebug.Initialize(() => _config.Get(), Monitor);

        EnsureNoLocalNestCoreDuplicates(helper);
        RegisterConsoleCommands(helper);

        helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        helper.Events.GameLoop.DayStarted += OnDayStarted;
        helper.Events.GameLoop.DayEnding += OnDayEnding;
        helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        helper.Events.GameLoop.Saving += OnSaving;
        helper.Events.Player.Warped += OnWarped;
        helper.Events.Input.ButtonPressed += OnButtonPressed;
        helper.Events.Multiplayer.ModMessageReceived += OnModMessageReceived;
        helper.Events.Display.RenderedHud += OnRenderedHud;
        InitializeModAssetHandler();
        InitializeContentPipeline();

        InstallHarmonyBootstrap();
        TryInstallDesertTombHarmonyPatches(reason: "startup");
    }

    public override object? GetApi()
    {
        if (_config is null)
            return null;

        _api ??= new WildEggsApi(
            () => _config.Get(),
            getStep: GetContentProgressionStep,
            getQuestCompletionDays: () => _contentProgression?.GetQuestCompletionDays()
                ?? new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase));
        SyncApiState();
        return _api;
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        if (_config is null)
            return;

        _gmcmHost = GmcmConfigHost<ModConfig>.TryCreate(
            helper: Helper,
            manifest: ModManifest,
            configProvider: _config,
            onSaved: OnGmcmSaved
        );

#if WEGG_DEV
        _devJsonTuning = new DevJsonTuningService(Helper, new SmapiLog(Monitor));
#endif

        InitializeWildAnimalsStardewUi();
        UpdateDiagnosticsState();
        RebuildGmcmLayout(force: true);
    }

    private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        _contentProgression?.OnDayStarted();
        _animalNotes?.OnDayStarted();
        _animalNotesClientObserver?.OnDayStarted();
        SyncApiState();
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        _contentProgression?.OnSaveLoaded();
        _animalNotes?.OnSaveLoaded();
        _animalNotesValidator?.ValidateAndLog();
        SyncApiState();
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (_config is null || !Context.IsWorldReady)
            return;

        if (Game1.activeClickableMenu is not null)
            return;

        var keybind = _config.Get().WildAnimalsEditorKeybind;
        if (keybind is not null && keybind.JustPressed())
        {
            OpenWildAnimalsEditor();
            return;
        }

        var progressionKeybind = _config.Get().AnimalProgressionEditorKeybind;
        if (progressionKeybind is null || !progressionKeybind.JustPressed())
            return;

        OpenAnimalProgressionEditor();
    }


    
private void OnConfigUpdated(object? sender, ModConfig _)
{
    SyncApiState();
    TryInstallDesertTombHarmonyPatches(reason: "config updated");
    UpdateDiagnosticsState();
}

private void SyncApiState()
    {
        if (_config is null || _api is null)
            return;

        _api.SetBirthTextMode(_config.Get().BirthText);

        var log = _contentLog ?? new SmapiLog(Monitor);
        var dataLoader = _contentDataLoader
            ?? new ConfiguredModDataLoader(new ModDataLoader(JsonFileReaderFactory.Create(Helper), log), _config, log);
        var snapshot = dataLoader.Load();
        var farmAnimals = Helper.GameContent.Load<Dictionary<string, FarmAnimalData>>(AssetNames.FarmAnimals);
        var catalog = WildEggSpawnCatalogBuilder.Build(snapshot.WildEggSpawnDefinitions, snapshot.WildAnimals, farmAnimals, Helper.ModRegistry, log);

        _api.SetWildEggsIDs(new HashSet<string>(
            catalog.ActiveEggDefinitions.Select(def => def.EggItemId),
            StringComparer.OrdinalIgnoreCase));

        _api.SetValidAnimals(ComputeParityVisibleAnimals(snapshot));

        _api.ClearChosenAnimals();
        SyncChosenAnimals(snapshot);
    }

    private HashSet<string> ComputeParityVisibleAnimals(ModDataSnapshot snapshot)
    {
        var visible = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (animalId, wild) in snapshot.WildAnimals)
        {
            if (wild.Enabled)
                visible.Add(animalId);
        }

        if (_config is null || !_config.Get().ProgressionMode || !Context.IsWorldReady)
            return visible;

        try
        {
            var farmAnimals = Helper.GameContent.Load<Dictionary<string, FarmAnimalData>>(AssetNames.FarmAnimals);
            var anchors = ShopAnchorIndex.Build(farmAnimals, warn: _ => { });

            foreach (var animalId in visible.ToList())
            {
                if (!farmAnimals.ContainsKey(animalId))
                {
                    visible.Remove(animalId);
                    continue;
                }

                if (!AnimalShopProgressionPolicy.ShouldGate(animalId, _config.Get(), farmAnimals, anchors))
                    continue;

                if (!Game1.MasterPlayer.mailReceived.Contains(AnimalNoteIds.NoteMailId(animalId)))
                    visible.Remove(animalId);
            }
        }
        catch
        {
            // Best-effort parity data: keep enabled animals if runtime assets are unavailable.
        }

        return visible;
    }

    private void SyncChosenAnimals(ModDataSnapshot snapshot)
    {
        if (_api is null || _chosenEggAssignments is null || !Context.IsWorldReady)
            return;

        try
        {
            var farmAnimals = Helper.GameContent.Load<Dictionary<string, FarmAnimalData>>(AssetNames.FarmAnimals);
            var anchors = ShopAnchorIndex.Build(farmAnimals, warn: _ => { });
            _chosenEggAssignments.RecomputeIfNeeded(GetContentProgressionStep(), farmAnimals, anchors);

            foreach (var pair in _chosenEggAssignments.Current.OrderBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase))
            {
                if (!farmAnimals.TryGetValue(pair.Value, out var animal) || animal is null)
                    continue;

                var isMagic = EggKindExtensions.FromEggItemId(pair.Key).IsMagic();
                _api.SetChosenAnimal(pair.Value, animal.House ?? string.Empty, isMagic);
            }
        }
        catch
        {
            // Best-effort parity data only.
        }
    }


    private static void EnsureNoLocalNestCoreDuplicates(IModHelper helper)
    {
        var localNestCore = System.IO.Path.Combine(helper.DirectoryPath, "NestCore.dll");
        if (System.IO.File.Exists(localNestCore))
            throw new InvalidOperationException("Local NestCore.dll detected under EggsInWild. Remove it and rely on the insNestCoreed NestCore mod.");
    }
}
