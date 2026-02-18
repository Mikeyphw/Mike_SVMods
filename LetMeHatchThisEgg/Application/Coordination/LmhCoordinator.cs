using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ContentPatcher;
using LetMeHatchThisEgg.Config;
using LetMeHatchThisEgg.Framework;
using LetMeHatchThisEgg.GameAssetManagement;
using LetMeHatchThisEgg.Infrastructure.Config;
using LetMeHatchThisEgg.Infrastructure.Logging;
using LetMeHatchThisEgg.UI;
using Mikew.BirdsOfAFeather.Framework;
using Mikew.BirdsOfAFeather.Helpers;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using static LetMeHatchThisEgg.EnumsAndStrings.Enums;
using static LetMeHatchThisEgg.EnumsAndStrings.Strings;
using static Mikew.BirdsOfAFeather.EnumsAndStrings.Strings;
using static Mikew.BirdsOfAFeather.Helpers.ModAssetHandler;

namespace LetMeHatchThisEgg.Application.Coordination;

public sealed class LmhCoordinator
{
    private readonly CoreContext _core;
    private readonly SmapiConfigProvider<ModConfig> _config;
    private readonly ILog _log;
    private readonly LetMeHatchApi _api;

    private IRoosterHatchingAPI? _roosterHatching;
    private IContentPatcherAPI? _contentPatcher;
    private IWildEggsApi? _wildEggs;

    private GameAssetManager? _assetManager;
    private GMCMenu? _gmcmMenu;

    private readonly List<string> _assetsRequested = new();
    private readonly HashSet<string> _addedTokens = new();

    private bool _earlyInitRequested;

    public LmhCoordinator(CoreContext core, SmapiConfigProvider<ModConfig> config, ILog log, LetMeHatchApi api)
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
    }

    public void OnWarped(object? sender, WarpedEventArgs e)
    {
        _core.Helper.GameContent.InvalidateCache(AssetNames.Machines);
    }

    public void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        if (_assetManager is null)
        {
            return;
        }

        if (_config.Config.CloneVanillaEggs && _assetManager.ItemsToAdd.Count <= 0)
        {
            _assetManager.ItemManager.CloneItemsFromGame();
        }

        _core.Helper.Data.WriteJsonFile("data/gray.json", _assetManager.TextureManager.GrayTextures.Keys);

        _core.Helper.GameContent.InvalidateCache(AssetNames.FarmAnimals);
        _core.Helper.GameContent.InvalidateCache(AssetNames.Objects);
        _core.Helper.GameContent.InvalidateCache(AssetNames.SpringObjects);
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

        InitializeGmcm();

        InvalidateGameContentCache();
        HandleBreedingData();

        _core.Helper.Events.Content.AssetRequested -= OnAssetRequested;
        _core.Helper.Events.Content.AssetRequested += OnAssetRequested;

        _gmcmMenu?.GMCMSetup();
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
    }

    private void InitializeGmcm()
    {
        if (_assetManager is null)
        {
            return;
        }

        var configMenuApi = _core.Helper.ModRegistry.GetApi<IGenericModConfigMenuAPI>(
            "spacechase0.GenericModConfigMenu"
        );
        var configExt = _core.Helper.ModRegistry.GetApi<IGMCMOptionsAPI>("jltaylor-us.GMCMOptions");

        _gmcmMenu = new GMCMenu(configExt, configMenuApi, _assetManager);
    }

    private void InvalidateGameContentCache()
    {
        if (_assetManager is null)
        {
            return;
        }

        _core.Helper.GameContent.InvalidateCache(AssetNames.FarmAnimals);
        _core.Helper.GameContent.InvalidateCache(AssetNames.Objects);

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
        if (_assetManager is null)
        {
            return;
        }

        var assetName = e.NameWithoutLocale.BaseName.ToLower();

        if (!_assetsRequested.Contains(assetName))
        {
            _assetsRequested.Add(assetName);
            _core.Helper.Data.WriteJsonFile("assetsRequested.json", _assetsRequested);
        }

        HandleAssetSpecificLogic(assetName, e);
    }

    private void HandleAssetSpecificLogic(string assetName, AssetRequestedEventArgs e)
    {
        if (_assetManager is null)
        {
            return;
        }

        if (Regex.IsMatch(assetName, @"^Animals/MaleClones", RegexOptions.IgnoreCase))
        {
            LoadTextureFromContentPack(assetName, e);
        }

        if (e.NameWithoutLocale.IsEquivalentTo(AssetNames.FarmAnimals))
        {
            HandleFarmAnimalsAsset(e);
        }

        if (e.NameWithoutLocale.IsEquivalentTo(AssetNames.Objects))
        {
            HandleObjectsAsset(e);
        }

        if (e.NameWithoutLocale.IsEquivalentTo(AssetNames.Machines))
        {
            HandleMachinesAsset(e);
        }

        if (ModEntry.AssetHandler.GamePaths.Contains(assetName))
        {
            HandleGamePathsAsset(assetName, e);
        }
    }

    private void LoadTextureFromContentPack(string assetName, AssetRequestedEventArgs e)
    {
        _assetManager!.TextureManager.LoadTextureFromContentPack(
            assetName,
            _assetManager!.ClonedAnimalsInfoList,
            _assetManager!.LoadedContentPacks,
            e,
            AssetLoadPriority.Exclusive
        );
    }

    private void HandleFarmAnimalsAsset(AssetRequestedEventArgs e)
    {
        if (_assetManager!.ClonedAnimalsInfoList.Any())
        {
            _assetManager.CreateClonePlaceholders(e, AssetEditPriority.Early);
            _assetManager.CreateAndAddAnimalClones(e, AssetEditPriority.Late);
        }

        HandleAdditionalFarmAnimalsLogic(e);
    }

    private void HandleAdditionalFarmAnimalsLogic(AssetRequestedEventArgs e)
    {
        var config = _config.Config;

        if (ModIdEvaluator.Evaluate("cantabilechaos.BFAVChickens", _core.Helper.ModRegistry) && config.CloneVanillaEggs)
        {
            // EditAssetWithFancyChickensEggs(e, AssetEditPriority.Late);
        }

        if (_assetManager!.ItemsToAdd.Count > 0)
        {
            _assetManager.ReplaceOriginalItemForClone(
                e,
                AssetEditPriority.Late + NEW_EGGS_ADD_PRIORITY
            );
        }

        if (config.EditEggListByName)
        {
            _assetManager.EditEggListByName(e, AssetEditPriority.Late + EDIT_EGG_LIST_PRIORITY);
        }

        if (config.EditBirthText)
        {
            _assetManager.EditBirthTexts(e, AssetEditPriority.Late + EDIT_BIRTH_TEXT_PRIORITY);
        }

        if (config.BreedingSystemEnabled && _assetManager.HasAddedMaleClones)
        {
            _assetManager.RunBreedingSystem(
                e,
                AssetEditPriority.Late + BREEDING_SYSTEM_PRIORITY
            );
        }
    }

    private void HandleObjectsAsset(AssetRequestedEventArgs e)
    {
        if (_assetManager!.ItemsToAdd.Count < 1)
        {
            return;
        }

        _assetManager.EditAssetWithNewClonedEggs(e, AssetEditPriority.Default);
        _assetManager.EditAssetWithNewFertilizedEggs(
            e,
            AssetEditPriority.Late + NEW_EGGS_ADD_PRIORITY
        );
    }

    private void HandleMachinesAsset(AssetRequestedEventArgs e)
    {
        var config = _config.Config;

        if (
            (config.OnlyFertilizedEggs && _assetManager!.HasAddedFertilizedEggs)
            || !config.OnlyFertilizedEggs
                && _assetManager!.HasAddedClonedItems
                && config.CloneMachineRules
        )
        {
            _assetManager.CloneMachineData(e, AssetEditPriority.Late + BASE_PRIORITY);
        }
    }

    private void HandleGamePathsAsset(string assetName, AssetRequestedEventArgs e)
    {
        if (assetName != ModEntry.AssetHandler.GetAssetPath(Assets.Object.ClonedItems, AssetPathType.GamePath))
        {
            ModEntry.AssetHandler.LoadModAssets(e);
        }
        else
        {
            _assetManager!.HandleClonedEggsAsset(e);
        }
    }
}
