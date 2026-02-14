using System.Text.RegularExpressions;
using Mikew.BirdsOfAFeather.Helpers;
using LetMeHatchThisEgg.Config;
using LetMeHatchThisEgg.Framework;
using LetMeHatchThisEgg.GameAssetManagement;
using LetMeHatchThisEgg.UI;
using Mikew.BirdsOfAFeather.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using static LetMeHatchThisEgg.EnumsAndStrings.Enums;
using static LetMeHatchThisEgg.EnumsAndStrings.Strings;
using static Mikew.BirdsOfAFeather.EnumsAndStrings.Strings;
using static Mikew.BirdsOfAFeather.Helpers.ModAssetHandler;

namespace LetMeHatchThisEgg
{
    /// <summary>The mod entry point.</summary>
    internal sealed partial class ModEntry : Mod
    {
        public override object GetApi() => letMeHatchApi;

        public readonly LetMeHatchApi letMeHatchApi = new();
        public List<string> AssetsRequested { get; private set; } = new();

        internal IRoosterHatchingAPI roosterHatching = null!;
        internal IContentPatcherAPI ContentPatcherAPI = null!;
        internal IWildEggsApi WildEggsApi = null!;
        internal static ModConfig Config { get; private set; } = new();
        public static IMonitor MonitorInstance { get; private set; } = null!;
        public static IModHelper HelperInstance { get; private set; } = null!;
        public static IManifest ModManifestInstance { get; private set; } = null!;
        public static Logger LoggerHelper { get; private set; } = null!;
        public static I18TranslationHelper TranslationHelper { get; private set; } = null!;
        public static ModAssetHandler AssetHandler { get; private set; } = null!;
        private GameAssetManager AssetManager = null!;
        private GMCMenu GMCMenu = null!;
        private HashSet<string> AddedTokens { get; set; } = new();

        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            InitializeStaticFields(Monitor, Helper, ModManifest, Helper.ReadConfig<ModConfig>());
            SetupEventHandlers();
            AddCustomCommands();
        }

        public static void InitializeStaticFields(
            IMonitor monitor,
            IModHelper helper,
            IManifest manifest,
            ModConfig config
        )
        {
            ModEntry.MonitorInstance = monitor;
            ModEntry.HelperInstance = helper;
            ModEntry.ModManifestInstance = manifest;
            ModEntry.Config = config;
            LoggerHelper = new(monitor, Config);
            TranslationHelper = new(helper);
            AssetHandler = new(manifest.UniqueID, monitor);
        }

        private void SetupEventHandlers()
        {
            Helper.Events.GameLoop.DayStarted += OnDayStart;
            Helper.Events.GameLoop.DayEnding += OnDayEnding;
            Helper.Events.GameLoop.UpdateTicked += HandleEarlyActions;
            Helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            Helper.Events.Player.Warped += OnWarped;
        }

        private void InitializeAssetEditor()
        {
            AssetManager = new(ContentPatcherAPI, WildEggsApi);
            AssetManager.TextureManager.SetUpModAssets();
        }

        public void AddCustomCommands()
        {
            Helper.ConsoleCommands.Add(
                "lmh_reloadcp",
                "Reloads the content packs for Let Me Hatch This Egg.\n\nUsage: lmh_reloadcp.",
                ReloadContentPacks
            );
            Helper.ConsoleCommands.Add(
                "lmh_checki18n",
                "Checks internationalization keys for the mod.\n\nUsage: lmh_checki18n.",
                CheckI18n
            );
        }

        public void ReloadContentPacks(string command, string[] args)
        {
            Helper.Events.GameLoop.UpdateTicked += HandleEarlyActions;
        }

        public void CheckI18n(string command, string[] args)
        {
            var jsonFile =
                Helper.Data.ReadJsonFile<Dictionary<string, string>>("i18n/default.json") ?? new();
            Helper.Data.WriteJsonFile("i18n/default.json.bak", jsonFile);
            TranslationHelper.Checki18nKeys(Monitor);
        }

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            ContentPatcherAPI = Helper.ModRegistry.GetApi<ContentPatcher.IContentPatcherAPI>(
                "Pathoschild.ContentPatcher"
            )!;
            //RegisterAnimalClonedItemsTokens();
            InitializeAPIs();
        }

        private void InitializeAPIs()
        {
            roosterHatching = Helper.ModRegistry.GetApi<IRoosterHatchingAPI>(
                "Mikew.roostersaddon.smapi"
            )!;
            WildEggsApi = Helper.ModRegistry.GetApi<IWildEggsApi>("Mikew.wildeggshatching")!;
        }

        private void InitializeGMCMenu()
        {
            var configMenuAPI = Helper.ModRegistry.GetApi<IGenericModConfigMenuAPI>(
                "spacechase0.GenericModConfigMenu"
            );
            var configExt = Helper.ModRegistry.GetApi<IGMCMOptionsAPI>("jltaylor-us.GMCMOptions");
            GMCMenu = new(configExt, configMenuAPI, AssetManager);
        }

        private void OnWarped(object? sender, WarpedEventArgs e)
        {
            Helper.GameContent.InvalidateCache(AssetNames.Machines);
        }

        private void OnDayStart(object? sender, DayStartedEventArgs e)
        {
            if (Config.CloneVanillaEggs && AssetManager.ItemsToAdd.Count <= 0)
            {
                AssetManager.ItemManager.CloneItemsFromGame();
            }

            Helper.Data.WriteJsonFile(
                $"data/gray.json",
                AssetManager.TextureManager.GrayTextures.Keys
            );
            Helper.GameContent.InvalidateCache(AssetNames.FarmAnimals);
            Helper.GameContent.InvalidateCache(AssetNames.Objects);
            Helper.GameContent.InvalidateCache(AssetNames.SpringObjects);
            // RegisterAnimalClonedItemsTokens();
        }

        private void OnDayEnding(object? sender, DayEndingEventArgs e)
        {
            AssetManager.DayEndingReset();
        }

        private void HandleEarlyActions(object? sender, UpdateTickedEventArgs e)
        {
            if (e.Ticks < 5)
            {
                return;
            }

            Helper.Events.GameLoop.UpdateTicked -= HandleEarlyActions;
            InitializeAssetEditor();
            letMeHatchApi.SetGameAssetManager(AssetManager);
            InitializeGMCMenu();

            InvalidateGameContentCache();
            HandleBreedingData();
            Helper.Events.Content.AssetRequested += OnAssetRequested;
            GMCMenu.GMCMSetup();
            letMeHatchApi.SetFinishedEarlyStuff(true);
        }

        private void InvalidateGameContentCache()
        {
            Helper.GameContent.InvalidateCache(AssetNames.FarmAnimals);
            Helper.GameContent.InvalidateCache(AssetNames.Objects);
            AssetManager.AnimalItemRelationsManager.HandleAnimalEggRelations();
            GameAssetManager.SaveData(AssetManager.SavedItemsData);
            AssetManager.ItemManager.CloneItemsFromGame();
        }

        private void HandleBreedingData()
        {
            AssetManager.BreedingDataLoader.LoadBreedingData();
            var loadedFemaleBreedingData = AssetManager.LoadedFemaleBreedingData;
            var breedingConfigsByPack = AssetManager.BreedingConfigsByPack;
            var clonedAnimalsInfoList = AssetManager.ClonedAnimalsInfoList;

            //LetMeHatchApi.Instance.SetClonedAnimalsInfo(clonedAnimalsInfoList);
            foreach (var (contentPackID, breedingDataFemaleDic) in loadedFemaleBreedingData)
            {
                breedingConfigsByPack.TryAdd(contentPackID, new());
                foreach (var (femaleKey, femaleBredingData) in breedingDataFemaleDic)
                {
                    breedingConfigsByPack[contentPackID].TryAdd(femaleKey, new(femaleBredingData));
                    foreach (var male in femaleBredingData.Males)
                    {
                        breedingConfigsByPack[contentPackID]
                            [femaleKey]
                            .BoostByMale.TryAdd(male.Key, male.Value.BoostByMale);
                        breedingConfigsByPack[contentPackID]
                            [femaleKey]
                            .MaleChance.TryAdd(male.Key, male.Value.MaleChance);
                        breedingConfigsByPack[contentPackID]
                            [femaleKey]
                            .Enabled.TryAdd(male.Key, male.Value.Enabled);
                    }
                }
            }
        }

        private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            var assetName = e.NameWithoutLocale.BaseName.ToLower();

            if (!AssetsRequested.Contains(assetName))
            {
                AssetsRequested.Add(assetName);
                Helper.Data.WriteJsonFile("assetsRequested.json", AssetsRequested);
            }

            HandleAssetSpecificLogic(assetName, e);
        }

        private void HandleAssetSpecificLogic(string assetName, AssetRequestedEventArgs e)
        {
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

            if (AssetHandler.GamePaths.Contains(assetName))
            {
                HandleGamePathsAsset(assetName, e);
            }
        }

        private void LoadTextureFromContentPack(string assetName, AssetRequestedEventArgs e)
        {
            AssetManager.TextureManager.LoadTextureFromContentPack(
                assetName,
                AssetManager.ClonedAnimalsInfoList,
                AssetManager.LoadedContentPacks,
                e,
                AssetLoadPriority.Exclusive
            );
        }

        private void HandleFarmAnimalsAsset(AssetRequestedEventArgs e)
        {
            if (AssetManager.ClonedAnimalsInfoList.Any())
            {
                AssetManager.CreateClonePlaceholders(e, AssetEditPriority.Early);
                AssetManager.CreateAndAddAnimalClones(e, AssetEditPriority.Late);
            }

            HandleAdditionalFarmAnimalsLogic(e);
        }

        private void HandleAdditionalFarmAnimalsLogic(AssetRequestedEventArgs e)
        {
            if (ModIdEvaluator.Evaluate("cantabilechaos.BFAVChickens", ModEntry.HelperInstance.ModRegistry) && Config.CloneVanillaEggs)
            {
                // EditAssetWithFancyChickensEggs(e, AssetEditPriority.Late);
            }

            if (AssetManager.ItemsToAdd.Count > 0)
            {
                AssetManager.ReplaceOriginalItemForClone(
                    e,
                    AssetEditPriority.Late + NEW_EGGS_ADD_PRIORITY
                );
            }

            if (Config.EditEggListByName)
            {
                AssetManager.EditEggListByName(e, AssetEditPriority.Late + EDIT_EGG_LIST_PRIORITY);
            }

            if (Config.EditBirthText)
            {
                AssetManager.EditBirthTexts(e, AssetEditPriority.Late + EDIT_BIRTH_TEXT_PRIORITY);
            }

            if (Config.BreedingSystemEnabled && AssetManager.HasAddedMaleClones)
            {
                AssetManager.RunBreedingSystem(
                    e,
                    AssetEditPriority.Late + BREEDING_SYSTEM_PRIORITY
                );
            }
        }

        private void HandleObjectsAsset(AssetRequestedEventArgs e)
        {
            if (AssetManager.ItemsToAdd.Count < 1)
            {
                return;
            }
            AssetManager.EditAssetWithNewClonedEggs(e, AssetEditPriority.Default);
            AssetManager.EditAssetWithNewFertilizedEggs(
                e,
                AssetEditPriority.Late + NEW_EGGS_ADD_PRIORITY
            );
        }

        private void HandleMachinesAsset(AssetRequestedEventArgs e)
        {
            if (
                (Config.OnlyFertilizedEggs && AssetManager.HasAddedFertilizedEggs)
                || !Config.OnlyFertilizedEggs
                    && AssetManager.HasAddedClonedItems
                    && Config.CloneMachineRules
            )
            {
                AssetManager.CloneMachineData(e, AssetEditPriority.Late + BASE_PRIORITY);
            }
        }

        private void HandleGamePathsAsset(string assetName, AssetRequestedEventArgs e)
        {
            if (
                assetName
                != AssetHandler.GetAssetPath(Assets.Object.ClonedItems, AssetPathType.GamePath)
            )
            {
                AssetHandler.LoadModAssets(e);
            }
            else
            {
                AssetManager.HandleClonedEggsAsset(e);
            }
        }

        private void RegisterAnimalClonedItemsTokens()
        {
            foreach (
                var (animalKey, clonedItems) in AssetManager.ItemsToAdd.Where(x =>
                    !AddedTokens.Contains(x.Key)
                )
            )
            {
                foreach (
                    var (itemType, items) in clonedItems.Select(i =>
                        (i.Key, i.Value.Select(x => x.Key))
                    )
                )
                {
                    foreach (var item in items)
                    {
                        Monitor.Log($"TokenName: {$"{animalKey}_{itemType}"}", LogLevel.Alert);
                        ContentPatcherAPI.RegisterToken(
                            ModManifest,
                            $"{animalKey}_{itemType}",
                            () =>
                            {
                                var value = new[] { item ?? string.Empty };
                                if (Context.IsWorldReady)
                                {
                                    return value;
                                }

                                if (SaveGame.loaded?.player != null)
                                {
                                    return value;
                                }

                                return null;
                            }
                        );
                    }
                }
                AddedTokens.Add(animalKey);
            }
        }
    }
}