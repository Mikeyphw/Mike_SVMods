using System;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using RoosterAddon.Constants;
using RoosterAddon.Enums;
using RoosterAddon.Framework;
using Mikew.BirdsOfAFeather.Framework;
using Mikew.BirdsOfAFeather.Helpers;
using RoosterAddon.Helpers;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.GameData.FarmAnimals;
using StardewValley.GameData.Locations;
using StardewValley.GameData.Objects;
using StardewValley.GameData.WorldMaps;

namespace RoosterAddon.Application
{
    /// <summary>The mod entry point.</summary>
    ///

    internal sealed class RoosterCoordinator : IRoosterCoordinator
    {
        private readonly ModEntry _host;
        private readonly RoosterAddon.Infrastructure.SaveData.ISaveDataStore _saveData;

        private IModHelper Helper => _host.Helper;
        private IMonitor Monitor => _host.Monitor;
        private IManifest ModManifest => _host\.ModManifest;

        private string SaveStateKey => $"{ModManifest.UniqueID}/save-state";

        internal ModConfig ModCfg = null!;
        internal
        internal IGenericModConfigMenuAPI? ConfigMenu;
        internal ILetMeHatchApi? HatchApi;

        internal Dictionary<Textures, Dictionary<TextureDetails, string>> ModTextures = new();

        private State.RoosterAddonSaveState _saveState = new();

        internal RoosterCoordinator(ModEntry host, RoosterAddon.Infrastructure.SaveData.ISaveDataStore saveData)
        {
            _host = host;
            _saveData = saveData;
        }

        internal void Initialize()
        {
            ModCfg = Helper.ReadConfig<ModConfig>();

            LoadRoosterFemales();
            LoadExclusionsData();
            ModCfg.FemalesOptions = new Dictionary<string, string>(_roosterFemales.Females);

            Helper.ConsoleCommands.Add(
                "roosteraddon_dump_state",
                "Write a JSON snapshot of RoosterAddon state to data/state-dumps.",
                DumpStateCommand
            );

            var texturesList = new List<Textures>
            {
                Textures.AllRoosters,
                Textures.BlueRooster,
                Textures.BrownRooster,
                Textures.GoldRooster,
                Textures.VoidRooster,
                Textures.WhiteRooster,
                Textures.VoidShopIcon,
                Textures.GoldShopIcon
            };

            foreach (var item in texturesList)
            {
                ModTextures.Add(
                    item,
                    new Dictionary<TextureDetails, string>
                    {
                        { TextureDetails.MoldFolderPath, "assets/" + ModConfig.GetEnumName(item).ToLower() + ".png" }
                    }
                );
            }

            RoosterHelper = new RoosterControl(ref ModCfg, Helper, Monitor);
        }


        // Mod configuration

        // Rooster helper

        // Mod configuration menu API
        // Content Patcher API

        // Conditions used by the Content Patcher API
        ManagedConditions? conditionSuperUnlucky, // Condition for super unlucky state
            conditionUnlucky, // Condition for unlucky state
            conditionNeutral, // Condition for neutral state
            conditionLucky, // Condition for lucky state
            conditionSuperLucky, // Condition for super lucky state
            conditionStuckInsideSeason, // Condition for being stuck inside during a specific season
            conditionStuckInsideWeather; // Condition for being stuck inside during specific weather

        // Flag indicating whether the player is stuck inside
        private bool isStuckInside;

        // Flag indicating whether the day has started
        private bool HasDayStarted = false;

        // Current daily luck type
        private LuckyTypes DailyLuck = LuckyTypes.Neutral; // Initialized to Neutral

        // Dictionary storing mod textures
        private RoosterFemales _roosterFemales = new();
        private readonly HashSet<string> _pendingInvalidations = new(StringComparer.OrdinalIgnoreCase);
        private readonly Queue<Action> _nextTickActions = new();
        private bool _nextTickHooked;
        private bool _gmcmRegistered;
        private bool _roosterFemalesDirty;
        private bool _needsRoosterSetup = true;
        private int _lastRoosterSetupDay = -1;
        private AnimalExclusion _animalExclusions = new();
        private int _lastDebugDumpDay = -1;


        /**********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        ///
        public bool HasModCheck(string modID)
        {
            return Helper.ModRegistry.IsLoaded(modID);
        }

        public static bool HasMod(string modID, IModHelper helper)
        {
            if (modID.Contains("&&"))
            {
                string[] mods = modID.Split("&&", StringSplitOptions.TrimEntries);
                for (int x = 0; x < mods.Length; x++)
                {
                    if (!helper.ModRegistry.IsLoaded(mods[x]))
                    {
                        return false;
                    }
                }
                return true;
            }
            if (modID.Contains("||"))
            {
                string[] mods = modID.Split("||", StringSplitOptions.TrimEntries);
                for (int x = 0; x < mods.Length; x++)
                {
                    if (helper.ModRegistry.IsLoaded(mods[x]))
                    {
                        return true;
                    }
                }
                return false;
            }
            return helper.ModRegistry.IsLoaded(modID);
        }

        // Entry point for the mod, called when the game is launched
        

        // Event handler for when the player warps to a new location
        public void OnWarped(object? sender, WarpedEventArgs e)
        {
            // Check if the update rate option is set to update on location change
            if (
                ModCfg.Options[OptionEntries.UpdateRate][OptionDetails.Value]
                == OptionValues.OnLocationChange
            )
            {
                // Invalidate the cache for farm animals data
                _ = Helper.GameContent.InvalidateCache("Data/FarmAnimals");
            }
        }

        public void OnDayStarted(object? sender, StardewModdingAPI.Events.DayStartedEventArgs e) => OnDayStart(sender, e);

        public void OnLocaleChanged(object? sender, LocaleChangedEventArgs e)
        {
            TryRegisterGmcm(force: true);
            RequestInvalidate(FarmAnimalsAssetName);
            RequestInvalidate(ObjectsAssetName);
        }

        /// <summary>The method called after a new day starts.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        // Event handler for when a new day starts
        private void OnDayStart(object? sender, DayStartedEventArgs e)
        {
            // Set flag to indicate that the day has started
            HasDayStarted = true;

            EnsureRoostersSetup();

            // Define raw conditions for different luck types and stuck inside conditions
            var rawSuperUnlucky = new Dictionary<string, string?>
            {
                ["Query: {{DailyLuck}} < -0.07"] = "true", // player is SuperUnlucky
            };
            var rawUnlucky = new Dictionary<string, string?>
            {
                ["Query: {{DailyLuck}} >= -0.07 AND {{DailyLuck}} < -0.02"] = "true", // player is Unlucky
            };
            var rawNeutral = new Dictionary<string, string?>
            {
                ["Query: {{DailyLuck}} >= -0.02 AND {{DailyLuck}} <= 0.02"] = "true", // player is Neutral
            };
            var rawLucky = new Dictionary<string, string?>
            {
                ["Query: {{DailyLuck}} > 0.02 AND {{DailyLuck}} <= 0.07"] = "true", // player is Lucky
            };
            var rawSuperLucky = new Dictionary<string, string?>
            {
                ["Query: {{DailyLuck}} > 0.07"] = "true", // player is SuperLucky
            };
            var rawStuckInsideSeason = new Dictionary<string, string?>
            {
                ["Season |contains=winter"] = "true",
            };
            var rawStuckInsideWeather = new Dictionary<string, string?>
            {
                ["Weather |contains=rain, storm, snow, greenrain"] = "true",
            };

            // Parse the raw conditions into managed conditions using ModEntry.ContentPatcher
            conditionSuperUnlucky = new ManagedConditions(ModEntry.ContentPatcher!.ParseConditions(
                manifest: ModManifest,
                rawConditions: rawSuperUnlucky,
                formatVersion: new SemanticVersion("2.0.0")
            ));
            conditionUnlucky = new ManagedConditions(ModEntry.ContentPatcher.ParseConditions(
                manifest: ModManifest,
                rawConditions: rawUnlucky,
                formatVersion: new SemanticVersion("2.0.0")
            ));
            conditionNeutral = new ManagedConditions(ModEntry.ContentPatcher.ParseConditions(
                manifest: ModManifest,
                rawConditions: rawNeutral,
                formatVersion: new SemanticVersion("2.0.0")
            ));
            conditionLucky = new ManagedConditions(ModEntry.ContentPatcher.ParseConditions(
                manifest: ModManifest,
                rawConditions: rawLucky,
                formatVersion: new SemanticVersion("2.0.0")
            ));
            conditionSuperLucky = new ManagedConditions(ModEntry.ContentPatcher.ParseConditions(
                manifest: ModManifest,
                rawConditions: rawSuperLucky,
                formatVersion: new SemanticVersion("2.0.0")
            ));
            conditionStuckInsideSeason = new ManagedConditions(ModEntry.ContentPatcher.ParseConditions(
                manifest: ModManifest,
                rawConditions: rawStuckInsideSeason,
                formatVersion: new SemanticVersion("2.0.0")
            ));
            conditionStuckInsideWeather = new ManagedConditions(ModEntry.ContentPatcher.ParseConditions(
                manifest: ModManifest,
                rawConditions: rawStuckInsideWeather,
                formatVersion: new SemanticVersion("2.0.0")
            ));

            // Update the context for each condition
            conditionSuperUnlucky.UpdateContext();
            conditionUnlucky.UpdateContext();
            conditionNeutral.UpdateContext();
            conditionLucky.UpdateContext();
            conditionSuperLucky.UpdateContext();
            conditionStuckInsideSeason.UpdateContext();
            conditionStuckInsideWeather.UpdateContext();

            // Check if the player is stuck inside
            isStuckInside =
                conditionStuckInsideSeason.IsMatch || conditionStuckInsideWeather.IsMatch;

            // Determine the player's daily luck based on the conditions
            if (conditionSuperUnlucky.IsMatch)
            {
                DailyLuck = LuckyTypes.SuperUnlucky;
            }
            else if (conditionUnlucky.IsMatch)
            {
                DailyLuck = LuckyTypes.Unlucky;
            }
            else if (conditionNeutral.IsMatch)
            {
                DailyLuck = LuckyTypes.Neutral;
            }
            else if (conditionLucky.IsMatch)
            {
                DailyLuck = LuckyTypes.Lucky;
            }
            else
            {
                DailyLuck = LuckyTypes.SuperLucky;
            }

            // Check if the update rate option is set to update on day start
            if (
                ModCfg.Options[OptionEntries.UpdateRate][OptionDetails.Value]
                == OptionValues.OnDayStart
            )
            {
                // Invalidate the cache for farm animals data
                _ = Helper.GameContent.InvalidateCache("Data/FarmAnimals");
            }
            // Get the hatch only by fertilized option
            // Create a new RoosterControl instance
            RoosterHelper.DailyLuck = DailyLuck;
        }

        // Commented out code to invalidate the cache for farm animals data
        //bool a= Helper.GameContent.InvalidateCache
        private const string FarmAnimalsAssetName = "Data/FarmAnimals";
        private const string ObjectsAssetName = "Data/Objects";

        public void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            if (!HasDayStarted)
                return;

            if (IsFarmAnimalsAsset(e))
            {
                EditFarmAnimalsAsset(e);
            }
            else if (IsObjectsAsset(e))
            {
                EditObjectsAsset(e);
            }
            else if (IsModTextureAsset(e))
            {
                LoadModTexture(e);
            }
        }

        private bool IsFarmAnimalsAsset(AssetRequestedEventArgs e)
        {
            return e.NameWithoutLocale.IsEquivalentTo(FarmAnimalsAssetName);
        }

        private bool IsObjectsAsset(AssetRequestedEventArgs e)
        {
            return e.NameWithoutLocale.IsEquivalentTo(ObjectsAssetName);
        }

        private bool IsModTextureAsset(AssetRequestedEventArgs e)
        {
            return e.NameWithoutLocale.BaseName.Contains("Mods/" + ModManifest.UniqueID);
        }

        // Event handler for when a farm animals asset is requested
        private void EditFarmAnimalsAsset(AssetRequestedEventArgs e)
        {
            // Edit the asset
            e.Edit(
                asset =>
                {
                    // Get the farm animal data from the asset
                    var data = asset.AsDictionary<string, FarmAnimalData>().Data;
                    EnsureRoostersSetup();
                    // Loop through each rooster and its females
                    foreach (var rooster in RoosterHelper.Roosters.Select(r => r.Value))
                    {
                        SetRoosterData(rooster, data);
                    }
                    // Loop through each rooster in the coop and reload it
                    foreach (var roosterFarmAnimal in RoosterControl.GetRoostersFromCoops())
                    {
                        roosterFarmAnimal.reload(roosterFarmAnimal.home);
                    }
                },
                AssetEditPriority.Late + 2000
            );
            // Hatching System
            e.Edit(
                asset =>
                {
                    var data = asset.AsDictionary<string, FarmAnimalData>().Data;
                    // Update the rooster quantity and get the roosters and females
                    RoosterHelper.UpdateRoosterQuantity_All();
                    GetRoostersFemales(data, RoosterHelper.Roosters);
                    UpdateRoosterHatchingApi();

                    // Update the rooster quantity and get the roosters and females
                    foreach (var rooster in RoosterHelper.Roosters.Select(r => r.Value))
                    {
                        RunHatchingSystem(rooster, data);
                    }
                
                    if (RoosterHelper.FemaleEggs.Count > 0)
                        RequestInvalidate(ObjectsAssetName);

                },
                AssetEditPriority.Late + 12500
            );
            e.Edit(
                asset =>
                {
                    var data = asset.AsDictionary<string, FarmAnimalData>().Data;
                    // Debugging
                    Debugging((Dictionary<string, FarmAnimalData>)data);
                },
                AssetEditPriority.Late + 10000
            );
        }

        // Debugging method
        private void Debugging(Dictionary<string, FarmAnimalData> data)
        {
            if (!Convert.ToBoolean(ModCfg.Options[OptionEntries.EnableDebug][OptionDetails.Value]))
                return;

            var today = SDate.Now().DaysSinceStart;
            if (_lastDebugDumpDay == today)
                return;

            _lastDebugDumpDay = today;

            var validAnimalKeys = new List<string>();
            foreach (var rooster in RoosterHelper.Roosters.Select(r => r.Value))
            {
                validAnimalKeys.Add(rooster.FarmAnimalName);
                validAnimalKeys.Add(rooster.Female);
                rooster.Females.ForEach(female => validAnimalKeys.Add(female));
            }

            var farmAnimals = data
                .Where(a => validAnimalKeys.Contains(a.Key))
                .ToDictionary(a => a.Key, a => a.Value);

            var roostersExport = new RoosterDataExport(RoosterHelper.Roosters);
            var farmAnimalsExport = new FarmAnimalsExport(farmAnimals);

            QueueNextTickAction(() =>
            {
                WriteStateDump(
                    $"{StardewModdingAPI.Constants.SaveFolderName}_roosters_{today}.json",
                    roostersExport
                );
                WriteStateDump(
                    $"{StardewModdingAPI.Constants.SaveFolderName}_farmAnimals_{today}.json",
                    farmAnimalsExport
                );
            });

            Logger.LogDailyLuck(DailyLuck, Monitor);
            Logger.LogRoosterData(RoosterHelper.Roosters, Monitor);
            Logger.LogFarmAnimals(farmAnimals, Monitor);
        }

        private void UpdateRoosterHatchingApi()
        {
            foreach (var rooster in RoosterHelper.Roosters.Select(r => r.Value))
            {
                // Ensure the female option exists for each female
                RoosterHatchingApi.Instance.UpdateChosenFemales(
                    _roosterFemales
                        .Females.Where(f => f.Value != OptionValues.Off)
                        .Select(f => f.Key)
                        .ToList()
                );
            }
        }

        /// <summary>
        /// Handles the AssetRequested event for objects.
        /// </summary>
        /// <param name="e">The event arguments.</param>
        private void EditObjectsAsset(AssetRequestedEventArgs e)
        {
            // Check if RoosterHelper or RoosterHelper.Roosters is null
            if (RoosterHelper == null || RoosterHelper.Roosters == null)
            {
                // If either is null, return without editing the asset
                return;
            }
            if (RoosterHelper.FemaleEggs.Count > 0)
            {
                e.Edit(
                    asset =>
                    {
                        // Get the object data from the asset
                        var dataObject = asset.AsDictionary<string, ObjectData>().Data;
                        var MearasFertilizedEggs = new List<string>
                        {
                            ItemIds.CustomItems.FertilizedBlueEgg,
                            ItemIds.CustomItems.FertilizedBrownEgg,
                            ItemIds.CustomItems.FertilizedGoldEgg,
                            ItemIds.CustomItems.FertilizedVoidEgg,
                            ItemIds.CustomItems.FertilizedWhiteEgg
                        };
                        // Extract egg IDs from FemaleEggs
                        var eggIdsFromFemaleEggs = RoosterHelper
                            .FemaleEggs.SelectMany(e => e.Value)
                            .Where(e => e.Key != EggTypeEntries.Fertilized)
                            .Select(e => e.Value)
                            .Distinct()
                            .ToList();
                        //Monitor.Log(string.Join(",", eggIdsFromFemaleEggs), LogLevel.Alert);

                        // Add Mearas fertilized egg IDs
                        var allEggIds = eggIdsFromFemaleEggs.Union(MearasFertilizedEggs).ToList();

                        // Filter dataObject by egg IDs
                        var OriginalEggs = dataObject
                            .Where(o => allEggIds.Contains(o.Key))
                            .ToDictionary(o => o.Key, o => o.Value);
                        GetAndSetFertilizedEggs(
                            (Dictionary<string, ObjectData>)dataObject,
                            OriginalEggs
                        );
                    },
                    AssetEditPriority.Late + 20000
                );
            } // Edit the asset
            e.Edit(
                asset =>
                {
                    // Get the object data from the asset
                    var dataObject = asset.AsDictionary<string, ObjectData>().Data;

                    // Add BetterTruffles tag to default rooster produces (small + deluxe)
                    var betterTrufflesIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var produces in RoosterHelper.Roosters.Values.Select(r => r.AllProduces))
                    {
                        betterTrufflesIds.Add(
                            produces[ProduceTypeEntries.Produce][ProduceEntries.First][
                                RoosterItemDataEntries.Name
                            ]
                        );
                        betterTrufflesIds.Add(
                            produces[ProduceTypeEntries.Deluxe][ProduceEntries.First][
                                RoosterItemDataEntries.Name
                            ]
                        );
                    }

                    foreach (var entry in dataObject)
                    {
                        if (!betterTrufflesIds.Contains(entry.Key))
                            continue;

                        entry.Value.ContextTags ??= new();
                        entry.Value.ContextTags.Add("temisthem_bettertruffles");
                    }

                },
                AssetEditPriority.Late + 10000
            );
        }

        private void LoadModTexture(AssetRequestedEventArgs e)
        {
            var path = ModTextures
                .Where(x => x.Value[TextureDetails.GamePath] == e.NameWithoutLocale.BaseName)
                .Select(a => a.Value[TextureDetails.MoldFolderPath])
                .Single();
            e.LoadFromModFile<Texture2D>(path!, AssetLoadPriority.Medium);
        }

        private bool GetHatchOnlyByFertilizedOption()
        {
            return ModCfg.Options[OptionEntries.HatchingMode][OptionDetails.Value]
                != OptionValues.Original;
        }

        private bool GetHatchApiCloneEggsOption()
        {
            return HatchApi != null && HatchApi.CloneEggs;
        }

        /// <summary>
        /// Sets the rooster data using the provided parameters.
        /// This method is responsible for setting various properties of a rooster, such as
        /// required building, harvest type, days to mature, days to produce, purchase price,
        /// sell price, unlock condition, and egg item ids. It also handles the logic for
        /// hatching roosters only by fertilized eggs and updating the percentage for rooster
        /// to chicken ratio.
        /// </summary>
        /// <param name="rooster">The rooster data to be set.</param>
        /// <param name="data">The dictionary containing farm animal data.</param>
        private void SetRoosterData(RoosterData rooster, IDictionary<string, FarmAnimalData> data)
        {
            // Set the required building, harvest type, days to mature, and days to produce
            data[rooster.FarmAnimalName].RequiredBuilding = rooster.BuildingRequired;
            data[rooster.FarmAnimalName].HarvestType =
                rooster.HarvestType == OptionValues.DigUp
                    ? FarmAnimalHarvestType.DigUp
                    : FarmAnimalHarvestType.DropOvernight;
            data[rooster.FarmAnimalName].DaysToMature = rooster.MatureDays;
            data[rooster.FarmAnimalName].DaysToProduce = rooster.ProduceDays;

            // Handle the blue egg logic if the mod "6480.blueegg" is present and the rooster is blue
            if (HasMod("6480.blueegg", Helper) && rooster.FarmAnimalName == RoosterKeys.Blue)
            {
                // Get the new blue egg translation
                var newBlueEgg = RoosterControl.ProduceTranslation(
                    ModCfg.RoosterOptions[RoosterType.Blue][OptionEntries.BlueEggChoice][
                        OptionDetails.Value
                    ]
                );

                // Set the blue egg produce item id
                RoosterHelper.Roosters[RoosterType.Blue].Eggs[EggTypeEntries.Normal][
                    RoosterItemDataEntries.Name
                ] = newBlueEgg;

                // Set the produce item id for the female rooster
                data[rooster.Female].ProduceItemIds = new List<FarmAnimalProduce>
                {
                    new() { ItemId = newBlueEgg, Id = newBlueEgg }
                };
            }

            // Set the purchase and sell price for buyable roosters
            if (rooster.Buyable)
            {
                // Calculate the purchase price
                data[rooster.FarmAnimalName].PurchasePrice =
                    rooster.FarmAnimalName == RoosterKeys.White
                        ? -1
                        : (int)Math.Round(rooster.Price / 2 / 5.0) * 5; // TODO round the result

                // Calculate the sell price
                data[rooster.FarmAnimalName].SellPrice =
                    (int)Math.Round(rooster.Price / 2 / 5.0) * 5; // TODO check sell price

                // Handle the unlock condition for the blue rooster
                if (
                    rooster.FarmAnimalName == RoosterKeys.Blue
                    && Convert.ToBoolean(
                        ModCfg.RoosterOptions[RoosterType.Blue][OptionEntries.BypassBlueConditions][
                            OptionDetails.Value
                        ]
                    )
                )
                {
                    data[rooster.FarmAnimalName].UnlockCondition = null;
                }

                // Handle the shop display name, description, and texture for the void and gold roosters
                if (rooster.FarmAnimalName == RoosterKeys.Void)
                {
                    data[rooster.FarmAnimalName].ShopDisplayName = Helper.Translation.Get(
                        "shop.void.displayname"
                    );
                    data[rooster.FarmAnimalName].ShopDescription = Helper.Translation.Get(
                        "shop.void.description"
                    );
                    data[rooster.FarmAnimalName].ShopTexture = ModTextures[Textures.VoidShopIcon][
                        TextureDetails.GamePath
                    ];
                }
                else if (rooster.FarmAnimalName == RoosterKeys.Gold)
                {
                    data[rooster.FarmAnimalName].ShopDisplayName = Helper.Translation.Get(
                        "shop.gold.displayname"
                    );
                    data[rooster.FarmAnimalName].ShopDescription = Helper.Translation.Get(
                        "shop.gold.description"
                    );
                    data[rooster.FarmAnimalName].ShopTexture = ModTextures[Textures.GoldShopIcon][
                        TextureDetails.GamePath
                    ];
                }
            }
            else
            {
                data[rooster.FarmAnimalName].PurchasePrice = -1;
            }

            ProduceRandomizationSystem(rooster, data);
            // Handle the logic for the stuck inside option
            if (GetEnableStuckInsideOption())
            {
                // Get the chosen harvest type
                var chosenHarvestType =
                    rooster.HarvestType == OptionValues.DigUp
                        ? FarmAnimalHarvestType.DigUp
                        : FarmAnimalHarvestType.DropOvernight;

                // Set the harvest type for the rooster based on the stuck inside option
                data[rooster.FarmAnimalName].HarvestType = isStuckInside
                    ? FarmAnimalHarvestType.DropOvernight
                    : chosenHarvestType;
            }
        }

        private void RunHatchingSystem(
            RoosterData rooster,
            IDictionary<string, FarmAnimalData> data
        )
        {
            var AnimalsThatShareEggs = AnimalsThatProduceRoosterEgg(
                (Dictionary<string, FarmAnimalData>)data,
                rooster
            );
            foreach (
                var female in rooster.Females.Where(f =>
                    _roosterFemales.Females[f].ToLower() == rooster.FarmAnimalName.ToLower()
                )
            )
            {
                // Handle the logic for hatching roosters only by fertilized eggs
                if (GetHatchOnlyByFertilizedOption())
                {
                    FertilizedHatchingSystem(female, rooster, data, AnimalsThatShareEggs);
                }
                else
                {
                    NormalHatchingSystem(female, rooster, data, AnimalsThatShareEggs);
                }
            }
        }

        private void FertilizedHatchingSystem(
            string female,
            RoosterData rooster,
            IDictionary<string, FarmAnimalData> data,
            List<string> OtherAnimals
        )
        {
            // Check if the fertilized egg probability is enabled
            if (
                RoosterControl.UpdatePercentage(
                    float.Parse(
                        rooster.Eggs[EggTypeEntries.Fertilized][RoosterItemDataEntries.Probability]
                    )
                )
            )
            {
                // Set the fertilized egg produce item id for the female roosters
                data[female].DeluxeProduceItemIds = new List<FarmAnimalProduce>
                {
                    new() { ItemId = RoosterHelper.FemaleEggs[female][EggTypeEntries.Fertilized] }
                };
                data[female].ProduceItemIds = new List<FarmAnimalProduce>
                {
                    new() { ItemId = RoosterHelper.FemaleEggs[female][EggTypeEntries.Fertilized] }
                };
            }
            // Clear the egg item ids for the rooster and its females
            data[rooster.FarmAnimalName].EggItemIds = null;
            data[female].EggItemIds = null;
            OtherAnimals.ForEach(a => data[a].EggItemIds = null);

            if (
                RoosterControl.UpdatePercentage(
                    Int32.Parse(
                        ModCfg.Options[OptionEntries.RoosterToChickenRatio][OptionDetails.Value]
                    )
                )
            )
            {
                // Set the fertilized egg item id for the rooster
                data[rooster.FarmAnimalName].EggItemIds ??= new();
                data[rooster.FarmAnimalName]
                    .EggItemIds.Add(
                        rooster.Eggs[EggTypeEntries.Fertilized][RoosterItemDataEntries.Name]
                    );
            }
            else
            {
                // Set the fertilized egg item id for the female roosters

                data[female].EggItemIds = new List<string>
                {
                    RoosterHelper.FemaleEggs[female][EggTypeEntries.Fertilized]
                };
            }
        }

        private void NormalHatchingSystem(
            string female,
            RoosterData rooster,
            IDictionary<string, FarmAnimalData> data,
            List<string> OtherAnimals
        )
        {
            // Initialize the egg item ids for the rooster and its females
            data[rooster.FarmAnimalName].EggItemIds ??= new();
            data[female].EggItemIds ??= new();

            // Check if the egg probability is enabled
            if (
                GetEnableEggProbabilityOption()
                && RoosterControl.UpdatePercentage(
                    Int32.Parse(
                        rooster.Eggs[EggTypeEntries.Normal][RoosterItemDataEntries.Probability]
                    )
                )
               
            )
            {
                // Add the normal egg item id to the rooster's egg item ids
                data[rooster.FarmAnimalName]
                    .EggItemIds.Add(
                        rooster.Eggs[EggTypeEntries.Normal][RoosterItemDataEntries.Name]
                    );
                data[female]
                    .EggItemIds.Remove(RoosterHelper.FemaleEggs[female][EggTypeEntries.Normal]);
                OtherAnimals.ForEach(a => data[a].EggItemIds.Remove(rooster.Eggs[EggTypeEntries.Normal][RoosterItemDataEntries.Name]));
            }

            // Check if the large egg probability is enabled
            if (
                GetEnableLargeEggProbabilityOption()
                && RoosterControl.UpdatePercentage(
                    Int32.Parse(
                        rooster.Eggs[EggTypeEntries.Large][RoosterItemDataEntries.Probability]
                    )
                )
               
            )
            {
                // Add the large egg item id to the rooster's egg item ids
                data[rooster.FarmAnimalName]
                    .EggItemIds.Add(
                        rooster.Eggs[EggTypeEntries.Large][RoosterItemDataEntries.Name]
                    );
                data[female]
                    .EggItemIds.Remove(RoosterHelper.FemaleEggs[female][EggTypeEntries.Large]);
                OtherAnimals.ForEach(a => data[a].EggItemIds.Remove(rooster.Eggs[EggTypeEntries.Large][RoosterItemDataEntries.Name]));
            }
        }

        private List<string> AnimalsThatProduceRoosterEgg(
            Dictionary<string, FarmAnimalData> animalData,
            RoosterData rooster
        )
        {
            // Extract the rooster egg IDs from the rooster data
            var roosterEggIds = rooster
                .Eggs.SelectMany(e => e.Value)
                .Where(e => e.Key == RoosterItemDataEntries.Name)
                .Select(e => e.Value)
                .ToList();

            // Filter the animal data to find animals that produce rooster eggs
            return animalData
                .Where(a => a.Value.EggItemIds.Any(e => roosterEggIds.Contains(e)))
                .Select(a => a.Key)
                .ToList();
        }

        private void ProduceRandomizationSystem(
            RoosterData rooster,
            IDictionary<string, FarmAnimalData> data
        )
        {
            // Handle the logic for rooster produce options
            if (rooster.ProduceOption != OptionValues.KeepOriginalProduce)
            {
                // Create a new farm animal produce item
                FarmAnimalProduce farmAnimalProduce =
                    new()
                    {
                        ItemId = rooster.AllProduces[ProduceTypeEntries.Produce][
                            ProduceEntries.Chosen
                        ][RoosterItemDataEntries.Name],
                        Id = rooster.AllProduces[ProduceTypeEntries.Produce][ProduceEntries.Chosen][
                            RoosterItemDataEntries.Name
                        ]
                    };

                // Set the produce item ids for the rooster
                data[rooster.FarmAnimalName].ProduceItemIds = new List<FarmAnimalProduce>
                {
                    farmAnimalProduce
                };
            }

            // Handle the logic for deluxe rooster produce options
            if (
                rooster.DeluxeProduceOption != OptionValues.Off
                && rooster.DeluxeProduceOption != OptionValues.KeepOriginalProduce
            )
            {
                // Create a new farm animal produce item
                FarmAnimalProduce deluxeFarmAnimalProduce =
                    new()
                    {
                        ItemId = rooster.AllProduces[ProduceTypeEntries.Deluxe][
                            ProduceEntries.Chosen
                        ][RoosterItemDataEntries.Name],
                        Id = rooster.AllProduces[ProduceTypeEntries.Deluxe][ProduceEntries.Chosen][
                            RoosterItemDataEntries.Name
                        ]
                    };

                // Set the deluxe produce item ids for the rooster
                data[rooster.FarmAnimalName].DeluxeProduceItemIds = new List<FarmAnimalProduce>
                {
                    deluxeFarmAnimalProduce
                };
            }
            else if (rooster.DeluxeProduceOption == OptionValues.Off)
            {
                data[rooster.FarmAnimalName].DeluxeProduceItemIds = null;
            }
        }

        private bool GetEnableEggProbabilityOption()
        {
            return Convert.ToBoolean(
                ModCfg.Options[OptionEntries.EnableEggProbability][OptionDetails.Value]
            );
        }

        private bool GetEnableLargeEggProbabilityOption()
        {
            return Convert.ToBoolean(
                ModCfg.Options[OptionEntries.EnableLargeEggProbability][OptionDetails.Value]
            );
        }

        private bool GetEnableStuckInsideOption()
        {
            return Convert.ToBoolean(
                ModCfg.Options[OptionEntries.EnableStuckInside][OptionDetails.Value]
            );
        }

        private void GetAndSetFertilizedEggs(
            Dictionary<string, ObjectData> data,
            Dictionary<string, ObjectData> originalEggs,
            bool mearasegg = false
        )
        {
            var RoosterDefaultFemales = RoosterHelper.Roosters.Select(r => r.Value.Female).ToList();

            if (GetHatchApiCloneEggsOption())
            {
                foreach (
                    var femaleEgg in RoosterHelper.FemaleEggs.Where(e =>
                        !RoosterDefaultFemales.Contains(e.Key)
                    )
                )
                {
                    var key = femaleEgg.Key;
                    var eggEntry = femaleEgg
                        .Value.Where(e => e.Key != EggTypeEntries.Fertilized)
                        .ToDictionary(x => x.Key, x => x.Value);
                    var fertilizedEggs = new Dictionary<string, string>();

                    foreach (
                        var (eggType, _eggFullID) in eggEntry.Where(e =>
                            data[e.Value].CustomFields != null
                            && data[e.Value].CustomFields.ContainsKey("originalEgg")
                        )
                    )
                    {
                        var smallEggs = new List<string> { "176", "180" };
                        var eggFullID = "";
                        string patternEgg = @"(^.*_)(\d+)$";
                        if (
                            smallEggs.Contains(Regex.Replace(_eggFullID, patternEgg, "$2"))
                            && CheckIfLargeEggExists(_eggFullID, data)
                        )
                        {
                            eggFullID = data[_eggFullID].CustomFields["originalEgg"] switch
                            {
                                "176" => Regex.Replace(_eggFullID, patternEgg, "$1") + "174",
                                _ => Regex.Replace(_eggFullID, patternEgg, "$1") + "182"
                            };
                        }
                        else
                        {
                            eggFullID = _eggFullID;
                        }
                        var egg = originalEggs[eggFullID];
                        if (mearasegg)
                        {
                            egg = originalEggs[GetFertilizedEggId(eggFullID)];
                        }
                        var newEgg = CreateFertilizedEgg(egg);
                        data[$"{eggFullID}_Fertilized"] = newEgg;
                        fertilizedEggs[eggFullID] = $"{eggFullID}_Fertilized";
                    }
                    foreach (var (keyEgg, value) in fertilizedEggs)
                    {
                        RoosterHelper.FemaleEggs[key][EggTypeEntries.Fertilized] = value;
                    }
                }
            }
            foreach (var defaultFemale in RoosterDefaultFemales)
            {
                var rooster = RoosterHelper
                    .Roosters.Select(r => r.Value)
                    .First(r => r.Female == defaultFemale);
                RoosterHelper.FemaleEggs[defaultFemale][EggTypeEntries.Fertilized] = rooster.Eggs[
                    EggTypeEntries.Fertilized
                ][RoosterItemDataEntries.Name];
            }
        }

        internal bool CheckIfLargeEggExists(string normalEgg, Dictionary<string, ObjectData> data)
        {
            string patternEgg = @"(^.*_)(\d+)$";
            var largeEgg = Regex.Replace(normalEgg, patternEgg, "$2") switch
            {
                "176" => "174",
                _ => "182"
            };
            var largeEggID = Regex.Replace(normalEgg, patternEgg, "$1") + largeEgg;
            return data.ContainsKey(largeEggID);
        }

        private string GetFertilizedEggId(string eggFullID)
        {
            string pattern = @"^.*_(\d+)$";
            string eggID = Regex.Replace(eggFullID, pattern, "$1");
            return eggID switch
            {
                ItemIds.AnimalProducts.BrownEgg
                or ItemIds.AnimalProducts.LargeBrownEgg
                    => ItemIds.CustomItems.FertilizedBrownEgg,
                ItemIds.AnimalProducts.WhiteEgg
                or ItemIds.AnimalProducts.LargeWhiteEgg
                    => ItemIds.CustomItems.FertilizedWhiteEgg,
                ItemIds.AnimalProducts.VoidEgg
                or ItemIds.AnimalProducts.LargeBrownEgg
                    => ItemIds.CustomItems.FertilizedVoidEgg,
                ItemIds.AnimalProducts.GoldenEgg => ItemIds.CustomItems.FertilizedGoldEgg,
                _ => ItemIds.AnimalProducts.WhiteEgg
            };
        }

        private ObjectData CreateFertilizedEgg(ObjectData egg)
        {
            List<string> contextTags =
                egg.ContextTags != null ? new List<string>(egg.ContextTags) : new List<string>();

            var newEgg = new ObjectData
            {
                ExcludeFromRandomSale = egg.ExcludeFromRandomSale,
                Type = egg.Type,
                Name = $"{Helper.Translation.Get("egg.fertilized.name")} {egg.DisplayName}",
                Description = Helper.Translation.Get("egg.fertilized.description"),
                Category = egg.Category,
                ContextTags = contextTags,
                ExcludeFromShippingCollection = true,
                Price = egg.Price * 2,
                SpriteIndex = egg.SpriteIndex,
                Texture = egg.Texture,
                Edibility = egg.Edibility + 10,
                DisplayName = $"{Helper.Translation.Get("egg.fertilized.name")} {egg.DisplayName}",
                CustomFields =
                    egg.CustomFields != null
                        ? new Dictionary<string, string>(egg.CustomFields)
                        : new Dictionary<string, string>(),
            };
            if (
                newEgg.CustomFields.TryGetValue(
                    "JollyLlama.PrismaticValleyFramework/Color",
                    out string? color
                )
            )
            {
                newEgg.CustomFields["JollyLlama.PrismaticValleyFramework/Color"] = "Custom Palette";
                newEgg.CustomFields["JollyLlama.PrismaticValleyFramework/Palette"] =
                    ColorGradientGenerator.GenerateGlowingColorGradient(color, 3);
            }
            return newEgg;
        }

        public void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            ModEntry.ContentPatcher = Helper.ModRegistry.GetApi<IContentPatcherAPI>(
                "Pathoschild.ContentPatcher"
            );

            HatchApi = Helper.ModRegistry.GetApi<ILetMeHatchApi>("Mikew.letmehatchthisegg.smapi");
            ConfigMenu = Helper.ModRegistry.GetApi<IGenericModConfigMenuAPI>(
                "spacechase0.GenericModConfigMenu"
            );

            TryRegisterGmcm(force: true);
        }

        /// <summary>
        /// Handles the UpdateTicked event.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void TryRegisterGmcm(bool force = false)
        {
            if (ConfigMenu is null)
                return;

            if (_gmcmRegistered && !force)
                return;

            if (_gmcmRegistered && force)
                ConfigMenu.Unregister(ModManifest);

            ConfigMenu.Register(
                mod: ModManifest,
                reset: () =>
                {
                    ModCfg = new ModConfig();
                    if (RoosterHelper is not null)
                        RoosterHelper.ModCfg = ModCfg;
                    Helper.WriteConfig(ModCfg);
                    ModCfg.FemalesOptions = new Dictionary<string, string>(_roosterFemales.Females);

                    _needsRoosterSetup = true;
                    RequestInvalidate(FarmAnimalsAssetName);
                    RequestInvalidate(ObjectsAssetName);
                },
                save: () =>
                {
                    if (RoosterHelper is not null)
                        RoosterHelper.ModCfg = ModCfg;
                    Helper.WriteConfig(ModCfg);

                    _roosterFemales.Females = new Dictionary<string, string>(ModCfg.FemalesOptions);
                    _roosterFemalesDirty = true;

                    _needsRoosterSetup = true;
                    RequestInvalidate(FarmAnimalsAssetName);
                    RequestInvalidate(ObjectsAssetName);
                }
            );

            ModCfg.FemalesOptions = new Dictionary<string, string>(_roosterFemales.Females);
            var gmcm = ConfigMenu;
            _host.GMCMSetup(ref gmcm);
            ConfigMenu = gmcm;

            _gmcmRegistered = true;
        }

        public void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            _saveState = _saveData.Read<State.RoosterAddonSaveState>(SaveStateKey) ?? new State.RoosterAddonSaveState();
            _needsRoosterSetup = true;
            _lastRoosterSetupDay = -1;
            _lastDebugDumpDay = -1;
        }

        public void OnSaving(object? sender, SavingEventArgs e)
        {
            SaveRoosterFemalesIfDirty();

            _saveState.LastRoosterSetupDay = _lastRoosterSetupDay;
            _saveState.LastDebugDumpDay = _lastDebugDumpDay;
            _saveState.LastKnownLuck = DailyLuck;
            _saveData.Write(SaveStateKey, _saveState);
        }

        public void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
        {
            SaveRoosterFemalesIfDirty();
            _gmcmRegistered = false;
            _pendingInvalidations.Clear();
            _nextTickActions.Clear();

            if (_nextTickHooked)
            {
                Helper.Events.GameLoop.UpdateTicked -= OnNextTick;
                _nextTickHooked = false;
            }
        }

        private void RequestInvalidate(string assetName)
        {
            if (string.IsNullOrWhiteSpace(assetName))
                return;

            _pendingInvalidations.Add(assetName);
            QueueNextTickAction(FlushInvalidations);
        }

        private void FlushInvalidations()
        {
            foreach (var name in _pendingInvalidations)
                _ = Helper.GameContent.InvalidateCache(name);

            _pendingInvalidations.Clear();
        }

        private void QueueNextTickAction(Action action)
        {
            _nextTickActions.Enqueue(action);
            if (_nextTickHooked)
                return;

            _nextTickHooked = true;
            Helper.Events.GameLoop.UpdateTicked += OnNextTick;
        }

        private void OnNextTick(object? sender, UpdateTickedEventArgs e)
        {
            while (_nextTickActions.Count > 0)
            {
                var action = _nextTickActions.Dequeue();
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    Monitor.Log($"Deferred action failed: {ex}", LogLevel.Error);
                }
            }

            Helper.Events.GameLoop.UpdateTicked -= OnNextTick;
            _nextTickHooked = false;
        }

        private void EnsureRoostersSetup()
        {
            if (!Context.IsWorldReady)
                return;

            var today = SDate.Now().DaysSinceStart;
            if (_needsRoosterSetup || _lastRoosterSetupDay != today)
            {
                RoosterHelper.SetUpRoosters();
                _lastRoosterSetupDay = today;
                _needsRoosterSetup = false;
            }
        }

        private void LoadRoosterFemales()
        {
            _roosterFemales = Helper.Data.ReadJsonFile<RoosterFemales>("roosterFemales.json")
                ?? new RoosterFemales();

            if (_roosterFemales.Females is null)
                _roosterFemales.Females = new Dictionary<string, string>();

            Helper.Data.WriteJsonFile("roosterFemales.json", _roosterFemales);
        }

        private void SaveRoosterFemalesIfDirty()
        {
            if (!_roosterFemalesDirty)
                return;

            try
            {
                Helper.Data.WriteJsonFile("roosterFemales.json", _roosterFemales);
                _roosterFemalesDirty = false;
            }
            catch (Exception ex)
            {
                Monitor.Log($"Failed to save roosterFemales.json: {ex}", LogLevel.Error);
            }
        }

        private void LoadExclusionsData()
        {
            var exclusions = Helper.Data.ReadJsonFile<AnimalExclusion>("exclusions.json")
                ?? new AnimalExclusion();

            Helper.Data.WriteJsonFile("exclusions.json", exclusions);

            var eggListExclusionsModID = exclusions.AnimalExclusionsModID ?? new();
            var animalExclusions = new List<string>();
            foreach (var animal in exclusions.AnimalExclusions ?? new())
                animalExclusions.AddRange(GetExclusions(animal));

            _animalExclusions = new AnimalExclusion
            {
                AnimalExclusions = animalExclusions.Distinct().ToList(),
                AnimalExclusionsModID = eggListExclusionsModID
            };
        }

        private void DumpStateCommand(string command, string[] args)
        {
            if (!Context.IsWorldReady)
            {
                Monitor.Log("Save not loaded; nothing to dump yet.", LogLevel.Info);
                return;
            }

            var includeFarmAnimals = args.Any(a => a.Equals("farmanimals", StringComparison.OrdinalIgnoreCase));
            var payload = BuildStateDump(includeFarmAnimals);
            var fileName = $"roosteraddon_state_{DateTimeOffset.Now:yyyyMMdd_HHmmss}.json";
            WriteStateDump(fileName, payload);

            Monitor.Log($"Wrote state dump: {fileName}", LogLevel.Info);
        }

        private object BuildStateDump(bool includeFarmAnimals)
        {
            var today = SDate.Now().DaysSinceStart;
            var roosters = RoosterHelper?.Roosters?.Values.Select(r => new
            {
                r.RoosterType,
                r.FarmAnimalName,
                r.Female,
                Females = r.Females?.ToList() ?? new List<string>(),
                Eggs = r.Eggs,
                ProduceOption = r.ProduceOption,
                ChosenProduce = r.ChosenProduce,
            }).ToList() ?? new();

            object? farmAnimals = null;
            if (includeFarmAnimals)
            {
                try
                {
                    var data = Helper.GameContent.Load<Dictionary<string, FarmAnimalData>>(FarmAnimalsAssetName);
                    farmAnimals = data.Keys.OrderBy(k => k).ToList();
                }
                catch (Exception ex)
                {
                    farmAnimals = new { error = ex.Message };
                }
            }

            return new
            {
                Timestamp = DateTimeOffset.Now,
                Mod = new { ModManifest.UniqueID, ModManifest.Version },
                Save = StardewModdingAPI.Constants.SaveFolderName,
                Day = new { today, Game1.Date.Season, Game1.Date.DayOfMonth, Game1.Date.Year },
                Luck = DailyLuck,
                Config = ModCfg,
                SaveState = _saveState,
                RoosterFemales = _roosterFemales.Females,
                Exclusions = _animalExclusions.AnimalExclusions,
                Roosters = roosters,
                FarmAnimals = farmAnimals
            };
        }

        private void WriteStateDump(string fileName, object payload)
        {
            var dumpDir = Path.Combine(Helper.DirectoryPath, "data", "state-dumps");
            Directory.CreateDirectory(dumpDir);

            var fullPath = Path.Combine(dumpDir, fileName);
            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(fullPath, json, Encoding.UTF8);
        }


        internal void GetRoostersFemales(
            IDictionary<string, FarmAnimalData> animalData,
            Dictionary<RoosterType, RoosterData> Roosters
        )
        {
            var eggTypes = Roosters
                .Values.SelectMany(rd => rd.Eggs.Values)
                .Where(ed => ed.ContainsKey(RoosterItemDataEntries.Name))
                .Select(ed => ed[RoosterItemDataEntries.Name])
                .ToList();

            var defaultFemales = Roosters.Values.ToDictionary(x => x.Female, x => x.FarmAnimalName);
            var foundFemales = defaultFemales.Keys.ToList();

            if (GetHatchApiCloneEggsOption() && HatchApi is not null)
            {
                foundFemales.AddRange(
                    GetFemales(animalData, eggTypes, _animalExclusions).OrderBy(f => f)
                );
            }

            var changed = false;

            foreach (var female in _roosterFemales.Females.Keys.ToList())
            {
                if (!foundFemales.Contains(female))
                {
                    _roosterFemales.Females.Remove(female);
                    changed = true;
                }
            }

            foreach (var female in foundFemales)
            {
                if (!_roosterFemales.Females.ContainsKey(female))
                {
                    _roosterFemales.Females[female] = defaultFemales.TryGetValue(female, out var def)
                        ? def
                        : OptionValues.Off;
                    changed = true;
                }
            }

            if (changed)
            {
                _roosterFemalesDirty = true;
                ModCfg.FemalesOptions = new Dictionary<string, string>(_roosterFemales.Females);
            }

            foreach (var roosterType in Roosters.Keys)
            {
                var roosterData = Roosters[roosterType];
                RoosterHelper.Roosters[roosterType].Females = _roosterFemales
                    .Females.Where(f =>
                        f.Value.Equals(roosterData.FarmAnimalName, StringComparison.OrdinalIgnoreCase)
                    )
                    .Select(f => f.Key)
                    .ToList();

                RoosterHelper.UpdateFemaleEggs(RoosterHelper.Roosters[roosterType], animalData);
            }
        }

        private List<string> GetFemales(
            IDictionary<string, FarmAnimalData> animalData,
            List<string> eggTypes,
            AnimalExclusion animalExclusions
        )
        {
            if (HatchApi is null)
                return new List<string>();

            return animalData
                .Where(a =>
                    a.Value.CustomFields != null
                    && a.Value.CustomFields.ContainsKey("ClonedEggs")
                    && GetClonedEggsList(a.Value.CustomFields["ClonedEggs"])
                        .Any(egg => eggTypes.Contains(egg, StringComparer.OrdinalIgnoreCase))
                    && !animalExclusions.AnimalExclusions.Contains(
                        a.Key,
                        StringComparer.OrdinalIgnoreCase
                    )
                    && HatchApi.AnimalsWithClonedEggs.Contains(a.Key)
                )
                .Select(a => a.Key)
                .ToList();
        }

        private List<string> GetClonedEggsList(string original)
        {
            var clonedEggsList = original
                .Replace(" ", "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .ToList();
            // clonedEggsList.ForEach(egg => Monitor.Log($"Cloned egg: {egg}"));
            return clonedEggsList;
        }

        private AnimalExclusion GetExclusions()
        {
            return _animalExclusions;
        }

        private static List<string> GetExclusions(string exclusionString)
        {
            if (string.IsNullOrEmpty(exclusionString))
            {
                return new List<string>();
            }

            var exclusionList = new List<string>();
            string[] exclusionsArray = exclusionString.Split(',');

            foreach (string exclusion in exclusionsArray)
            {
                if (exclusion.Contains(','))
                {
                    exclusionList.AddRange(GetExclusions(exclusion));
                }
                else
                {
                    exclusionList.Add(exclusion.Trim());
                }
            }

            return exclusionList;
        }
    }
}
