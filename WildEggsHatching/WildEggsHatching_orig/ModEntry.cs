using BirdsOfAFeather.Helpers;
using Microsoft.Xna.Framework;
using Mikew.BirdsOfAFeather.Framework;
using Mikew.BirdsOfAFeather.Helpers;
using Mikew.BirdsOfAFeather.Models;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Monsters;
using WildEggsHatching.Commands;
using WildEggsHatching.DesertTomb;
using WildEggsHatching.DesertTomb.Core;
using WildEggsHatching.DesertTomb.TombMonsters.Creatures;
using WildEggsHatching.Framework;
using WildEggsHatching.Management;
using WildEggsHatching.Managers;
using WildEggsHatching.Managers.GameAssetManagement;
using WildEggsHatching.Services;
using WildEggsHatching.UI;
using static Mikew.BirdsOfAFeather.EnumsAndStrings.Strings;
using static Mikew.BirdsOfAFeather.Helpers.ModAssetHandler;
using static WildEggsHatching.EnumsAndStrings.Enums;
using static WildEggsHatching.EnumsAndStrings.Enums.Assets;
using static WildEggsHatching.EnumsAndStrings.Enums.TextOptionValues;
using static WildEggsHatching.EnumsAndStrings.Strings;
using static WildEggsHatching.Management.ProgressionManager;

namespace WildEggsHatching
{
    /// <summary>The mod entry point.</summary>
    ///
    internal sealed partial class ModEntry : Mod
    {
        internal static ModConfig Config { get; private set; } = new();
        internal static IMonitor MonitorInstance { get; private set; } = null!;
        internal static IModHelper HelperInstance { get; private set; } = null!;
        internal static IManifest ModManifestInstance { get; private set; } = null!;
        public static Logger LoggerHelper { get; private set; } = null!;
        public static I18TranslationHelper TranslationHelper { get; private set; } = null!;
        public static ModAssetHandler AssetHandler { get; private set; } = null!;
        public bool PharaohChanging { get; private set; } = false;

        internal ILetMeHatchApi? LetMeHatchApi = null;
        private DesertTombWrapper DesertTomb = null!;
        private WildListManager WildListManager = null!;
        private GMCMenu GMCMenu = null!;
        private ProgressionManager ProgressionManager = null!;
        private GameAssetManager GameAssetManager = null!;
        private EggSpawning EggSpawn = null!;
        private CustomCommands CustomCommands;
        private const string AdditionalMineMapsDic = "aedenthorn.AdditionalMineMaps/dictionary";
        internal static string ModID = string.Empty;

        /**********
        ** internal methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        ///
        public override object GetApi()
        {
            return WildEggsApi.Instance;
        }

        public override void Entry(IModHelper helper)
        {
            MonitorInstance = Monitor;
            HelperInstance = Helper;
            ModManifestInstance = ModManifest;
            InitializeStaticFields(Monitor, Helper, ModManifest, helper.ReadConfig<ModConfig>());
            SetUpModID(ModManifest);
            SetUpModAssets();

            Helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            Helper.Events.GameLoop.UpdateTicked += HandleEarlyStuff;
            WildEggsApi.Instance.SetWildEggsIDs(
                new()
                {
                    IDs.Objects.FarmWEgg,
                    IDs.Objects.FarmLWEgg,
                    IDs.Objects.FarmMWEgg,
                    IDs.Objects.FarmLMWEgg,
                }
            );
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

        private static void SetUpModID(IManifest manifest)
        {
            ModID = manifest.UniqueID;
        }

        private void SetUpModAssets()
        {
            var assetsToAdd = new List<AssetData>(
                new List<AssetData>
                {
                    //new(typeof(AnimalName), AssetFolder.AnimalIcons, FileExtension.Png, null),
                    new(typeof(Assets.Object), AssetFolder.Objects, FileExtension.Png, null),
                    new(
                        typeof(Assets.BigCraftable),
                        AssetFolder.BigCraftables,
                        FileExtension.Png,
                        null
                    ),
                    new(typeof(Assets.Misc), AssetFolder.Misc, FileExtension.Png, null),
                    new(
                        typeof(Assets.MineTexture),
                        AssetFolder.Mines,
                        FileExtension.Png,
                        AssetNames.Mines
                    ),
                    new(
                        typeof(Assets.MineMonster),
                        AssetFolder.Mines,
                        FileExtension.Png,
                        "Characters/Monsters"
                    ),
                    new(
                        typeof(Assets.MineLevel),
                        AssetFolder.Mines,
                        FileExtension.Tmx,
                        AssetNames.Mines
                    ),
                }
            );
            AssetHandler.AddAssets(assetsToAdd);
            AssetHandler.SetUpAssets();
        }

        public void HandleEarlyStuff(object? sender, UpdateTickedEventArgs e)
        {
            if (
                (LetMeHatchApi == null && e.Ticks >= 4)
                || (LetMeHatchApi != null && LetMeHatchApi.FinishedEarlyStuff)
            )
            {
                WildListManager = new(LetMeHatchApi);
                WildListManager.SetUpAnimalLists();
                Config.PopulateAnimalProgressionMode(WildListManager.ValidWildAnimals);
                Helper.WriteConfig(Config);

                Helper.Events.GameLoop.UpdateTicked -= HandleEarlyStuff;

                ProgressionManager = new(WildListManager);
                GameAssetManager = new(LetMeHatchApi, WildListManager, ProgressionManager);
                DesertTomb = new();
                GetGMCMApi();

                GMCMenu.Setup();
                CustomCommands = new CustomCommands(
                    WildListManager,
                    GMCMenu,
                    ProgressionManager,
                    GameAssetManager
                );
                CustomCommands.AddCustomCommands();
                EggSpawn = new(ProgressionManager.ModSaveData);
                Helper.Events.GameLoop.DayStarted += OnDayStart;
                Helper.Events.GameLoop.DayEnding += OnDayEnding;
                Helper.Events.Player.Warped += OnWarped;
                Helper.Events.Content.AssetRequested += OnAssetRequested;
                Helper.Events.GameLoop.Saving += OnSaving;
            }
        }

        /// <summary>The method called after a new day starts.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        public void OnDayStart(object? sender, DayStartedEventArgs e)
        {
            ProgressionManager.ModSaveData.WeatherToday = ProgressionManager
                .ModSaveData
                .WeatherTomorrow;
            ProgressionManager.ModSaveData.WeatherTomorrow = Game1.weatherForTomorrow;
            //WildListManager.OnDayStart();

            //LogChosenAnimals();
            DesertTomb.OnDayStart();
            ProgressionManager.OnDayStart();
            WildEggsApi.Instance.SetBirthTextMode(Config.BirthText.ToString());

            Helper.GameContent.InvalidateCache(AssetNames.Objects);

            GameAssetManager.ChooseAnimalsForAllEggs();

            Helper.GameContent.InvalidateCache(AssetNames.FarmAnimals);
            Helper.GameContent.InvalidateCache(AssetNames.Mail);
            Helper.GameContent.InvalidateCache(AssetNames.Quests);
            Helper.GameContent.InvalidateCache(AssetNames.SpecialOrders);
            Helper.GameContent.InvalidateCache(AssetNames.Monsters);
            Helper.GameContent.InvalidateCache(AssetNames.BigCraftables);
            Helper.GameContent.InvalidateCache(AssetNames.CraftingRecipes);
            Helper.GameContent.InvalidateCache(AssetNames.Machines);
            Helper.GameContent.InvalidateCache(AdditionalMineMapsDic);

            EggSpawn.OnDayStart(GameAssetManager.WildEggs, WildListManager.ValidWildAnimals);
            CustomCommands.SetEggSpawning(EggSpawn);
        }

        public void OnDayEnding(object? sender, DayEndingEventArgs e)
        {
            //WildAnimalManager.OnDayEnding();
            DesertTomb.OnDayEnding();
            GameAssetManager.OnDayEnding();
            ProgressionManager.OnDayEnding();
        }

        public void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            var wildEggIncubatorSprite = AssetHandler.GetAssetPath(
                Assets.BigCraftable.WildEggIncubator,
                AssetPathType.GamePath
            );

            string assetName = e.NameWithoutLocale.BaseName.ToLower();

            if (e.NameWithoutLocale.IsEquivalentTo(AssetNames.FarmAnimals))
            {
                EditFarmAnimalsAsset(e);
            }

            if (e.NameWithoutLocale.IsEquivalentTo(AssetNames.Objects))
            {
                EditObjectsAsset(e);
            }

            if (e.NameWithoutLocale.IsEquivalentTo(AssetNames.BigCraftables))
            {
                EditBigCraftableAsset(e);
            }

            if (e.NameWithoutLocale.IsEquivalentTo(AssetNames.Machines))
            {
                EditMachineAsset(e);
            }

            if (e.NameWithoutLocale.IsEquivalentTo("Data/CraftingRecipes"))
            {
                EditCraftingRecipesAsset(e);
            }

            if (e.NameWithoutLocale.IsEquivalentTo(AssetNames.SpecialOrders) && Game1.hasStartedDay)
            {
                EditSpecialOrderAsset(e);
            }

            if (e.NameWithoutLocale.IsEquivalentTo(AssetNames.Mail) && Game1.hasStartedDay)
            {
                EditMailAsset(e);
            }
            if (e.NameWithoutLocale.IsEquivalentTo(AssetNames.Monsters))
            {
                EditMonstersAsset(e);
            }

            if (e.NameWithoutLocale.IsEquivalentTo(AssetNames.Quests) && Game1.hasStartedDay)
            {
                EditQuestsAsset(e);
            }

            // if (DesertTomb.ChosenMineLevels.Select(l => l.Asset).Contains(assetName))
            //  {
            //      LoadMineLevelAsset(e, assetName);
            //  }
            //DesertTomb.ShouldEditMineLevel()

            if (e.NameWithoutLocale.IsEquivalentTo(AdditionalMineMapsDic))
            {
                LoadMineLevelAsset(e, assetName);
            }
            if (AssetHandler.GamePaths.Contains(assetName))
            {
                AssetHandler.LoadModAssets(e);
            }

            if (assetName == wildEggIncubatorSprite && !Config.NeedsFern)
            {
                EditWildEggIncubatorAsset(e);
            }
        }

        private void EditWildEggIncubatorAsset(AssetRequestedEventArgs e)
        {
            GameAssetManager.EditWildEggIncubatorAsset(e, AssetEditPriority.Late);
        }

        private void EditQuestsAsset(AssetRequestedEventArgs e)
        {
            GameAssetManager.AddQuests(e, AssetEditPriority.Default);
        }

        private void EditMailAsset(AssetRequestedEventArgs e)
        {
            GameAssetManager.AddMail(e, AssetEditPriority.Default);
        }

        private void EditSpecialOrderAsset(AssetRequestedEventArgs e)
        {
            GameAssetManager.AddSpecialOrders(e, AssetEditPriority.Default);
        }

        private void EditCraftingRecipesAsset(AssetRequestedEventArgs e)
        {
            GameAssetManager.AddCraftingRecipes(e, AssetEditPriority.Default);
        }

        private void EditMachineAsset(AssetRequestedEventArgs e)
        {
            LoggerHelper.VerboseLog("Editing mahchines...");
            GameAssetManager.AddMachineOutputRules(e, AssetEditPriority.Default);
            GameAssetManager.AddMachineOutputRulesForEggs(e, AssetEditPriority.Late);
            GameAssetManager.EditOtherIncubators(e, AssetEditPriority.Late + 100);
        }

        private void EditBigCraftableAsset(AssetRequestedEventArgs e)
        {
            GameAssetManager.AddBigCraftables(e, AssetEditPriority.Default);
        }

        private void EditMonstersAsset(AssetRequestedEventArgs e)
        {
            e.Edit(
                asset =>
                {
                    var data = asset.AsDictionary<string, string>().Data;
                    data["Spinner"] =
                        "30/5/0/0/false/1000/771 .9 771 .5 770 .5 382 .1 86 .005 72 .001/1/.01/3/2/.00/true/5/Spinner";
                    data["The Pharaoh"] =
                        "260/30/0/0/false/0/769 .75 769 .2 337 .002 336 .01 335 .02 334 .04 108 .003 -4 .1 98 .005 99 .001 74 .0005/2/.01/8/3/.0/true/15/The Pharaoh";
                },
                AssetEditPriority.Early
            );
        }

        private void EditFarmAnimalsAsset(AssetRequestedEventArgs e)
        {
            GameAssetManager.AddVanillaAnimalPrices(e, AssetEditPriority.Late + 10);

            GameAssetManager.AddWildEggToChosenAnimals(e, AssetEditPriority.Late);

            GameAssetManager.ProgressionModeEdits(e, AssetEditPriority.Late + 2000);
        }

        private void EditObjectsAsset(AssetRequestedEventArgs e)
        {
            GameAssetManager.AddItems(e, AssetEditPriority.Default);

            if (Config.WildEggEdit && Game1.objectData.ContainsKey(IDs.Objects.FarmWEgg))
            {
                GameAssetManager.EditWildEgg(e, AssetEditPriority.Late + 10);
            }
        }

        // private void LoadMineLevelAsset(AssetRequestedEventArgs e, string assetName)
        // {
        //     if (DesertTomb.TombLevelsRelation == null || DesertTomb.IsFirstMineEdit)
        //     {
        //         return;
        //     }
        //     string[] parts = assetName.Split('/');
        //     int number = int.Parse(parts[^1]);
        //     var mineLevel = DesertTomb
        //         .TombLevelsRelation.FirstOrDefault(x => x.Value.Contains(number))
        //         .Key;
        //     var assetPath = AssetHandler.GetAssetPath(mineLevel, AssetPathType.ModFolderPath);
        //     e.LoadFromModFile<Map>(assetPath, AssetLoadPriority.Medium);
        // }
        private void LoadMineLevelAsset(AssetRequestedEventArgs e, string assetName)
        {
            var quantity = ModEntry.Config.TombChance switch
            {
                TombChance.Low => 5,
                TombChance.Medium => 10,
                TombChance.High => 15,
                TombChance.Always => 39,
                _ => 1,
            };

            var levels = GetRandomizedLevels();
            e.Edit(
                asset =>
                {
                    var data = asset.AsDictionary<string, AdditionalMineMaps.MapData>().Data;

                    for (int i = 0; i < quantity; i++)
                    {
                        data[$"DesertTomb_{i}"] = new AdditionalMineMaps.MapData()
                        {
                            forceLevel = -1,
                            mapPath = AssetHandler.GetAssetPath(
                                MineLevel.Level77377,
                                AssetPathType.GamePath
                            ),
                            mapType = "M",
                            maxLevel = -1,
                            minLevel = 121,
                        };
                    }
                },
                AssetEditPriority.Default
            );
            // // Log the entry into the method
            // LoggerHelper.TestingLog($"LoadMineLevelAsset called with assetName: {assetName}");

            // // Check if TombLevelsRelation is null or if it's the first mine edit
            // if (DesertTomb.TombLevelsRelation == null)
            // {
            //     LoggerHelper.TestingLog("Exiting method: TombLevelsRelation is null.");
            //     return;
            // }

            // if (DesertTomb.IsFirstMineEdit)
            // {
            //     LoggerHelper.TestingLog("Exiting method: It's the first mine edit.");
            //     return;
            // }

            // // Split the assetName to get the parts
            // string[] parts = assetName.Split('/');
            // LoggerHelper.TestingLog($"Asset name parts: {string.Join(", ", parts)}");

            // // Ensure that there are enough parts to parse the number
            // if (parts.Length == 0)
            // {
            //     LoggerHelper.TestingLog("Exiting method: No parts found in assetName.");
            //     return;
            // }

            // // Parse the number from the last part
            // int number;
            // try
            // {
            //     number = int.Parse(parts[^1]);
            //     LoggerHelper.TestingLog($"Parsed number: {number}");
            // }
            // catch (FormatException ex)
            // {
            //     LoggerHelper.TestingLog(
            //         $"Error parsing number from assetName: {assetName}. Exception: {ex.Message}"
            //     );
            //     return;
            // }
            // catch (Exception ex)
            // {
            //     LoggerHelper.TestingLog($"Unexpected error while parsing number: {ex.Message}");
            //     return;
            // }

            // // Find the mine level
            // var mineLevelEntry = DesertTomb.TombLevelsRelation.FirstOrDefault(x =>
            //     x.Value.Contains(number)
            // );
            // if (mineLevelEntry.Key == null) // Replace YourKeyType and YourValueType with actual types
            // {
            //     LoggerHelper.TestingLog(
            //         $"Exiting method: No mine level found for number: {number}"
            //     );
            //     return;
            // }

            // var mineLevel = mineLevelEntry.Key;
            // LoggerHelper.TestingLog($"Found mine level: {mineLevel}");

            // // Get the asset path
            // var assetPath = AssetHandler.GetAssetPath(mineLevel, AssetPathType.ModFolderPath);
            // LoggerHelper.TestingLog($"Asset path: {assetPath}");

            // // Load the asset
            // e.LoadFromModFile<Map>(assetPath, AssetLoadPriority.High);
            // LoggerHelper.TestingLog("Asset loading initiated.");
        }

        private List<Assets.MineLevel> GetRandomizedLevels()
        {
            var excludedLevels = new Assets.MineLevel[] { };

            return EnumExtensions
                .GetValues<Assets.MineLevel>()
                .Where(x => !excludedLevels.Contains(x))
                .OrderBy(x => Game1.random.Next())
                .ToList();
        }

        public void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            LetMeHatchApi = Helper.ModRegistry.GetApi<ILetMeHatchApi>("Mikew.lmh");
        }

        private void GetGMCMApi()
        {
            var gMCMenu = Helper.ModRegistry.GetApi<IGenericModConfigMenuAPI>(
                "spacechase0.GenericModConfigMenu"
            );
            if (gMCMenu is null)
                return;
            var configExt = Helper.ModRegistry.GetApi<IGMCMOptionsAPI>("jltaylor-us.GMCMOptions");
            var configMenu = new IGMCMOptionsHelper(
                configExt,
                gMCMenu,
                TranslationHelper,
                ModManifest
            );
            GMCMenu = new(
                configMenu,
                ProgressionManager.ModSaveData,
                WildListManager,
                GameAssetManager
            );
        }

        /* private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
         {
             // get Generic Mod Config Menu's API (if it's installed)
             if (e.Ticks <= 4)
             {
                 return;
             }
             Helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;
             GMCMenu.Setup();
         }
 */
        public void OnSaving(object? sender, SavingEventArgs e)
        {
            ProgressionManager.OnDayEnding();
            ProgressionManager.ModSaveData.WeatherYesterday = ProgressionManager
                .ModSaveData
                .WeatherToday;
            GameAssetManager.ProcessWildEggs(e => e.Spawned = false);
        }

        public void OnWarped(object? sender, WarpedEventArgs e)
        {
            var location = e.NewLocation;
            ProcessHatchedAnimals();
            HandleMineLocation(location, e.Player);
        }

        private void ProcessHatchedAnimals()
        {
            ProgressionManager.ExecuteIfProgressionAllows(
                ProgressionStep.PartVI,
                () => ProgressionManager.ProcessHatchedAnimals()
            );
        }

        // private void HandleMineLocation(GameLocation location)
        // {
        //     if (
        //         ProgressionManager.GetCurrentStep() == ProgressionStep.PartIV
        //         || Config.KeepMineLevel
        //     )
        //     {
        //         if (ShouldEditMineLevel(location))
        //         {
        //             EditMineLevel(location);
        //         }
        //         else if (IsFirstMineEdit(location))
        //         {
        //             DesertTomb.IsFirstMineEdit = false;
        //         }
        //     }
        // }

        // private bool ShouldEditMineLevel(GameLocation location)
        // {
        //     return DesertTomb.ShouldEditMineLevel(location) && !DesertTomb.IsFirstMineEdit;
        // }

        // private void EditMineLevel(GameLocation location)
        // {
        //     DesertTomb.SetUpTombHelpers((MineShaft)location);
        //     DesertTomb.Fire();
        // }

        // private bool IsFirstMineEdit(GameLocation location)
        // {
        //     return DesertTomb.IsFirstMineEdit
        //         && location is MineShaft mineShaft
        //         && DesertTomb
        //             .ChosenMineLevels.Select(l => l.Number)
        //             .Contains(mineShaft.loadedMapNumber);
        // }
        private void HandleMineLocation(GameLocation location, Farmer player)
        {
            LoggerHelper.TestingLog($"HandleMineLocation called with location: {location}");
            if (
                ProgressionManager.GetCurrentStep() == ProgressionStep.PartIV
                || Config.KeepMineLevel
            )
            {
                LoggerHelper.TestingLog("Current step is Part IV or KeepMineLevel is enabled.");
                DesertTomb.IsFirstMineEdit = false;
                //DesertTomb.ShouldEditMineLevel(location) &&
                if (location.mapPath.Value.StartsWith("data/mines"))
                {
                    LoggerHelper.TestingLog(
                        "ShouldEditMineLevel returned true. Proceeding to edit mine level."
                    );
                    //Helper.Events.Display.RenderingHud += RenderPharaohTracker;
                    EditMineLevel(location);
                    Helper.Events.Display.RenderingHud += RenderPharaohTracker;
                }
                else if (IsFirstMineEdit(location))
                {
                    LoggerHelper.TestingLog(
                        "IsFirstMineEdit returned true. Setting IsFirstMineEdit to false."
                    );
                    DesertTomb.IsFirstMineEdit = false;
                }
                else
                {
                    LoggerHelper.TestingLog(
                        "Neither ShouldEditMineLevel nor IsFirstMineEdit returned true."
                    );
                    Helper.Events.Display.RenderingHud -= RenderPharaohTracker;
                }
            }
            else
            {
                LoggerHelper.TestingLog(
                    "Exiting HandleMineLocation: Current step is not Part IV and KeepMineLevel is not enabled."
                );
            }
        }

        private void RenderPharaohTracker(object? sender, RenderingHudEventArgs e)
        {
            DesertTomb.PharaohDrawArrowTracker(e.SpriteBatch);
        }

        private void EditMineLevel(GameLocation location)
        {
            LoggerHelper.TestingLog($"EditMineLevel called with location: {location.Name}");
            DesertTomb = new();
            DesertTomb.OnDayStart();
            DesertTomb.OnDayStart();
            DesertTomb.IsFirstMineEdit = false;

            DesertTomb.SetUpTombHelpers((MineShaft)location);
            DesertTomb.Fire();
            LoggerHelper.TestingLog("EditMineLevel completed and Fire method invoked.");
            // Calculate pixel position from tile coordinates
            float pixelX = (29 + 0.5f) * 64f;
            float pixelY = (96 + 0.5f) * 64f;
            Game1.player.Position = new Vector2(pixelX, pixelY);
        }

        private bool IsFirstMineEdit(GameLocation location)
        {
            return DesertTomb.IsFirstMineEdit && location is MineShaft;
        }
    }
}
