using System;
using LetMeHatch.Application;
using LetMeHatch.Application.Gateways;
using LetMeHatch.Infrastructure.Gateways;
using LetMeHatch.Infrastructure.ApplicationAdapters;
using System.Collections.Generic;
using System.Linq;
using LetMeHatch.Config;
using LetMeHatch.Framework;
using LetMeHatch.GameAssetManagement;
using Mikew.NestCore.Helpers.Config;
using Mikew.NestCore.Helpers.Logging;
using LetMeHatch.Infrastructure.Content;
using Mikew.NestCore.Helpers.Content;
using LetMeHatch.Integrations.Gmcm;
using Mikew.NestCore.Framework;
using Mikew.NestCore.Framework.Random;
using Mikew.NestCore.Helpers;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using static LetMeHatch.EnumsAndStrings.Enums;
using static LetMeHatch.EnumsAndStrings.Strings;
using static Mikew.NestCore.EnumsAndStrings.Strings;
using LogScopedHelper = Mikew.NestCore.Helpers.Logging.LogScopedHelper;
using ScopedLogCatalog = Mikew.NestCore.Helpers.Logging.ScopedLogCatalog;

namespace LetMeHatch.Application.Coordination;

public sealed class LmhCoordinator
{
    private readonly CoreContext _core;
    private readonly SmapiConfigProvider<ModConfig> _config;
    private readonly ILog _log;
    private readonly AssetInvalidationBatcher _invalidation;
    private readonly LetMeHatchApi _api;

    private IRoosterHatchingAPI? _roosterHatching;
    private IWildEggsApi? _wildEggs;

    private GameAssetManager? _assetManager;
    private LmhApplicationServices? _app;
    private AssetPipelineHost<ContentContext>? _assetPipeline;

    private readonly HashSet<string> _addedTokens = new();

    private bool _earlyInitRequested;

    public LmhCoordinator(CoreContext core, SmapiConfigProvider<ModConfig> config, ILog log, LetMeHatchApi api, AssetInvalidationBatcher invalidation)
    {
        _core = core ?? throw new ArgumentNullException(nameof(core));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _api = api ?? throw new ArgumentNullException(nameof(api));
        _invalidation = invalidation ?? throw new ArgumentNullException(nameof(invalidation));
    }

    public void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
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

    public void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        RequestEarlyInit();
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

#if MIKE_DEV
        _core.Helper.Data.WriteJsonFile("data/gray.json", _assetManager.TextureManager.GrayTextures.Keys);
#endif

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
        if (!_earlyInitRequested)
        {
            return;
        }

        if (!Context.IsWorldReady || e.Ticks < 5)
        {
            return;
        }

        if (_assetManager is not null)
        {
            _core.Helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;
            _earlyInitRequested = false;
            return;
        }

        _core.Helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;
        _earlyInitRequested = false;

        if (_wildEggs is null)
        {
            LogScopedHelper.Log(_log, ScopedLogCatalog.ModLthteCategory.Runtime, "Skipping early initialization because Wild Eggs API is unavailable.", LogLevel.Warn);
            return;
        }

        InitializeAssetEditor();
        _api.SetGameAssetManager(_assetManager!);


        InvalidateGameContentCache();
        HandleBreedingData();

        _assetPipeline?.Attach();
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

        ModEntry.TranslationHelper.Checki18nKeys(_log);
    }


    public void PrintContentHandlers()
    {
        if (_assetPipeline is null)
        {
            LogScopedHelper.Log(_log, ScopedLogCatalog.ModLthteCategory.ContentPipeline, "Content pipeline is not initialized yet.", LogLevel.Warn);
            return;
        }

        LogScopedHelper.Log(
            _log,
            ScopedLogCatalog.ModLthteCategory.ContentPipeline,
            "NestCore asset pipeline host is initialized and attached on early init.",
            LogLevel.Info);
    }

    public void InvalidateAllContent()
    {
        if (_assetPipeline is null)
        {
            LogScopedHelper.Log(_log, ScopedLogCatalog.ModLthteCategory.ContentPipeline, "Content pipeline is not initialized yet.", LogLevel.Warn);
            return;
        }

        _assetPipeline.RequestInvalidateAll();

        LogScopedHelper.Log(_log, ScopedLogCatalog.ModLthteCategory.ContentPipeline, "Invalidated caches for all registered content handlers.", LogLevel.Info);
    }

    public void PrintStatus()
    {
        var lines = new List<string>
        {
            $"WorldReady={Context.IsWorldReady}",
            $"MainPlayer={Context.IsMainPlayer}",
            $"EarlyInitRequested={_earlyInitRequested}",
            $"WildEggsApiReady={_wildEggs is not null}",
            $"RoosterApiReady={_roosterHatching is not null}",
            $"AssetManagerReady={_assetManager is not null}",
            $"AssetPipelineReady={_assetPipeline is not null}",
            $"ApplicationServicesReady={_app is not null}",
        };

        foreach (var line in lines)
            LogScopedHelper.Log(_log, ScopedLogCatalog.ModLthteCategory.Runtime, line, LogLevel.Info);
    }

    private void InitializeApis()
    {
        _roosterHatching = _core.Helper.ModRegistry.GetApi<IRoosterHatchingAPI>("Mikew.roostersaddon.smapi");
        _wildEggs = _core.Helper.ModRegistry.GetApi<IWildEggsApi>("Mikew.eggsinwild");

        if (_roosterHatching is null)
        {
            throw new InvalidOperationException(
                "LetMeHatch requires Rooster's Addon (Mikew.roostersaddon.smapi). Install/enable it and try again."
            );
        }

        if (_wildEggs is null)
        {
            throw new InvalidOperationException(
                "LetMeHatch requires EggsInWild (Mikew.eggsinwild). Install/enable it and try again."
            );
        }
    }

private void InitializeAssetEditor()
{
    if (_wildEggs is null)
    {
        return;
    }

    _assetManager = new GameAssetManager(_wildEggs);
    ModEntry.CurrentAssetManager = _assetManager;
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

}
