using System;
using LetMeHatchThisEgg.Application;
using LetMeHatchThisEgg.Application.Gateways;
using LetMeHatchThisEgg.Infrastructure.Gateways;
using LetMeHatchThisEgg.Infrastructure.ApplicationAdapters;
using System.Collections.Generic;
using System.Linq;
using ContentPatcher;
using LetMeHatchThisEgg.Config;
using LetMeHatchThisEgg.Framework;
using LetMeHatchThisEgg.GameAssetManagement;
using Mikew.BirdsOfAFeather.Helpers.Config;
using Mikew.BirdsOfAFeather.Helpers.Logging;
using LetMeHatchThisEgg.Infrastructure.Content;
using Mikew.BirdsOfAFeather.Helpers.Content;
using LetMeHatchThisEgg.Integrations.Gmcm;
using Mikew.BirdsOfAFeather.Framework;
using Mikew.BirdsOfAFeather.Framework.Random;
using Mikew.BirdsOfAFeather.Helpers;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using static LetMeHatchThisEgg.EnumsAndStrings.Enums;
using static LetMeHatchThisEgg.EnumsAndStrings.Strings;
using static Mikew.BirdsOfAFeather.EnumsAndStrings.Strings;

namespace LetMeHatchThisEgg.Application.Coordination;

public sealed class LmhCoordinator
{
    private readonly CoreContext _core;
    private readonly SmapiConfigProvider<ModConfig> _config;
    private readonly ILog _log;
    private readonly AssetInvalidationBatcher _invalidation;
    private readonly LetMeHatchApi _api;

    private IRoosterHatchingAPI? _roosterHatching;
    private IContentPatcherAPI? _contentPatcher;
    private IWildEggsApi? _wildEggs;

    private GameAssetManager? _assetManager;
    private LmhApplicationServices? _app;
    private LmhAssetPipeline? _assetPipeline;

    private readonly List<string> _assetsRequested = new();
    private readonly HashSet<string> _addedTokens = new();

    private bool _earlyInitRequested;

    public LmhCoordinator(CoreContext core, SmapiConfigProvider<ModConfig> config, ILog log, LetMeHatchApi api, AssetInvalidationBatcher invalidation)
    {
        _core = core ?? throw new ArgumentNullException(nameof(core));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _api = api ?? throw new ArgumentNullException(nameof(api));
    }

    public void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        _contentPatcher = _core.Helper.ModRegistry.GetApi<ContentPatcher.IContentPatcherAPI>(
            "Pathoschild.ContentPatcher"
        );

        if (_contentPatcher is null)
        {
            throw new InvalidOperationException(
                "LetMeHatchThisEgg requires Content Patcher (Pathoschild.ContentPatcher). Install/enable it and try again."
            );
        }

        InitializeApis();

        LmhGmcmMenu.TryRegister(
            core: _core,
            configProvider: _config,
            onSaved: () =>
            {
                InvalidateAllContent();
                InvalidateGameContentCache();
            }
        );
    }

    public void OnWarped(object? sender, WarpedEventArgs e)
    {
        _invalidation.Request(AssetNames.Machines);
    }

    public void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        if (_assetManager is null)
        {
            return;
        }

        if (_config.Get().CloneVanillaEggs && _assetManager.ItemsToAdd.Count <= 0)
        {
            _assetManager.ItemManager.CloneItemsFromGame();
        }

        _core.Helper.Data.WriteJsonFile("data/gray.json", _assetManager.TextureManager.GrayTextures.Keys);

        _invalidation.Request(AssetNames.FarmAnimals);
        _invalidation.Request(AssetNames.Objects);
        _invalidation.Request(AssetNames.SpringObjects);
    }

    public void OnDayEnding(object? sender, DayEndingEventArgs e)
    {
        _assetManager?.DayEndingReset();
    }

    public void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (e.Ticks < 5)
        {
            return;
        }

        if (!_earlyInitRequested && _assetManager is not null)
        {
            _core.Helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;
            return;
        }

        _core.Helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;
        _earlyInitRequested = false;

        if (_contentPatcher is null || _wildEggs is null)
        {
            throw new InvalidOperationException(
                "Early initialization ran before required integrations were available. This should not happen; please report this."
            );
        }

        InitializeAssetEditor();
        _api.SetGameAssetManager(_assetManager!);


        InvalidateGameContentCache();
        HandleBreedingData();

        _core.Helper.Events.Content.AssetRequested -= OnAssetRequested;
        _core.Helper.Events.Content.AssetRequested += OnAssetRequested;
        _api.SetFinishedEarlyStuff(true);
    }

    public void RequestEarlyInit()
    {
        _earlyInitRequested = true;

        _core.Helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;
        _core.Helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
    }

    public void CheckI18n()
    {
        var jsonFile = _core.Helper.Data.ReadJsonFile<Dictionary<string, string>>("i18n/default.json") ?? new();
        _core.Helper.Data.WriteJsonFile("i18n/default.json.bak", jsonFile);

        ModEntry.TranslationHelper.Checki18nKeys(_core.Monitor);
    }


    public void PrintContentHandlers()
    {
        if (_assetPipeline is null)
        {
            _log.Log("Content pipeline is not initialized yet.", LogLevel.Warn);
            return;
        }

        foreach (var line in _assetPipeline.GetDebugReportLines())
            _log.Log(line, LogLevel.Info);
    }

    public void InvalidateAllContent()
    {
        if (_assetPipeline is null)
        {
            _log.Log("Content pipeline is not initialized yet.", LogLevel.Warn);
            return;
        }

        _assetPipeline.RequestInvalidateAll();

        _log.Log("Invalidated caches for all registered content handlers.", LogLevel.Info);
    }

    private void InitializeApis()
    {
        _roosterHatching = _core.Helper.ModRegistry.GetApi<IRoosterHatchingAPI>("Mikew.roostersaddon.smapi");
        _wildEggs = _core.Helper.ModRegistry.GetApi<IWildEggsApi>("Mikew.wildeggshatching");

        if (_roosterHatching is null)
        {
            throw new InvalidOperationException(
                "LetMeHatchThisEgg requires Rooster's Addon (Mikew.roostersaddon.smapi). Install/enable it and try again."
            );
        }

        if (_wildEggs is null)
        {
            throw new InvalidOperationException(
                "LetMeHatchThisEgg requires WildEggsHatching (Mikew.wildeggshatching). Install/enable it and try again."
            );
        }
    }

private void InitializeAssetEditor()
{
    if (_contentPatcher is null || _wildEggs is null)
    {
        return;
    }

    _assetManager = new GameAssetManager(_contentPatcher, _wildEggs);
    _assetManager.TextureManager.SetUpModAssets();

    _app = new LmhApplicationServices(
        new GameRandomProvider(),
        new LmhGateways(
            new SmapiGameDataGateway(_core.Helper.GameContent),
            new SmapiWorldGateway(),
            new SmapiClock(),
            new SmapiAssetGateway(_invalidation)
        ),
        new FarmAnimalsAssetEditsAdapter(_assetManager),
        new ObjectsAssetEditsAdapter(_assetManager),
        new MachinesAssetEditsAdapter(_assetManager),
        new AssetLoadsAdapter(_assetManager)
    );


    _assetPipeline = LetMeHatchContentPipeline.Create(
        _core,
        _config,
        _log,
        ModEntry.AssetHandler,
        _assetManager,
        _app
        , _invalidation
    );
}

    private void InvalidateGameContentCache()
    {
        if (_assetManager is null)
        {
            return;
        }

        _invalidation.Request(AssetNames.FarmAnimals);
        _invalidation.Request(AssetNames.Objects);

        _assetManager.AnimalItemRelationsManager.HandleAnimalEggRelations();
        GameAssetManager.SaveData(_assetManager.SavedItemsData);
        _assetManager.ItemManager.CloneItemsFromGame();
    }

    private void HandleBreedingData()
    {
        if (_assetManager is null)
        {
            return;
        }

        _assetManager.BreedingDataLoader.LoadBreedingData();

        var loadedFemaleBreedingData = _assetManager.LoadedFemaleBreedingData;
        var breedingConfigsByPack = _assetManager.BreedingConfigsByPack;

        foreach (var (contentPackID, breedingDataFemaleDic) in loadedFemaleBreedingData)
        {
            breedingConfigsByPack.TryAdd(contentPackID, new());

            foreach (var (femaleKey, femaleBreedingData) in breedingDataFemaleDic)
            {
                breedingConfigsByPack[contentPackID].TryAdd(femaleKey, new(femaleBreedingData));

                foreach (var male in femaleBreedingData.Males)
                {
                    breedingConfigsByPack[contentPackID][femaleKey].BoostByMale.TryAdd(male.Key, male.Value.BoostByMale);
                    breedingConfigsByPack[contentPackID][femaleKey].MaleChance.TryAdd(male.Key, male.Value.MaleChance);
                    breedingConfigsByPack[contentPackID][femaleKey].Enabled.TryAdd(male.Key, male.Value.Enabled);
                }
            }
        }
    }

        private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        if (_assetManager is null || _assetPipeline is null)
        {
            return;
        }

        var assetName = e.NameWithoutLocale.BaseName.ToLowerInvariant();

        if (!_assetsRequested.Contains(assetName))
        {
            _assetsRequested.Add(assetName);
            _core.Helper.Data.WriteJsonFile("assetsRequested.json", _assetsRequested);
        }

        _assetPipeline.HandleAssetRequested(e);
    }
}
