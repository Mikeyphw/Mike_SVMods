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

    private readonly Dictionary<string, ExtraModAnimalKeysBulkState> _extraModAnimalKeysBulkByWildAnimalKey = new(StringComparer.OrdinalIgnoreCase);
    private HashSet<string> _knownLocationTypeKeysForNormalization = new(StringComparer.OrdinalIgnoreCase);

    private DesertTombRuntime? _desertTombRuntime;
    private bool _desertTombHarmonyInstalled;

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
            configProvider: _config,
            onSaved: OnGmcmSaved
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


    private static void EnsureNoLocalNestCoreDuplicates(IModHelper helper)
    {
        var localNestCore = System.IO.Path.Combine(helper.DirectoryPath, "NestCore.dll");
        if (System.IO.File.Exists(localNestCore))
            throw new InvalidOperationException("Local NestCore.dll detected under EggsInWild. Remove it and rely on the insNestCoreed NestCore mod.");
    }
}
