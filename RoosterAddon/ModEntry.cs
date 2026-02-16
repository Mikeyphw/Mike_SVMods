using System;
using System.ComponentModel.DataAnnotations;
using System.Text;
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

namespace RoosterAddon
{
    /// <summary>The mod entry point.</summary>
    ///

    internal sealed partial class ModEntry : Mod
    {
        // Mod configuration
        internal ModConfig ModCfg = null!; // Reference to the mod's configuration, initialized to null

        // Rooster helper
        RoosterControl RoosterHelper = null!; // Helper class for Rooster-related functionality, initialized to null

        // Mod configuration menu API
        internal IGenericModConfigMenuAPI configMenu = null!; // Reference to the mod's configuration menu API, initialized to null
        internal ILetMeHatchApi hatchApi = null!;

        // Content Patcher API
        internal static IContentPatcherAPI? ContentPatcher; // Static reference to the Content Patcher API, which allows modifying game content

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
        private Dictionary<Textures, Dictionary<TextureDetails, string>> ModTextures = new(); // Keys represent texture types, values contain texture details and strings
        private RoosterFemales _roosterFemales = new();

        private AnimalExclusion _animalExclusions = new();
        private bool _roosterFemalesDirty;
        private bool _roosterFemalesSaveScheduled;
        private bool _objectsCacheInvalidationScheduled;
        private readonly Queue<Action> _nextTickActions = new();
        private bool _isNextTickHandlerSubscribed;

        /**********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        ///
        public bool HasModCheck(string modID)
        {
            return this.Helper.ModRegistry.IsLoaded(modID);
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

        public override object GetApi()
        {
            return RoosterHatchingApi.Instance;
        }

        // Entry point for the mod, called when the game is launched
        public override void Entry(IModHelper helper)
        {
            // Read the mod's configuration from the file
            ModCfg = this.Helper.ReadConfig<ModConfig>();

            EnsureExclusionsLoaded();
            EnsureRoosterFemalesLoaded();

            // Subscribe to game events
            helper.Events.GameLoop.GameLaunched += OnGameLaunched; // Called when the game is launched
            // UpdateTicked is subscribed only for one-shot scheduled actions (see ScheduleNextTick)
this.Helper.Events.GameLoop.DayStarted += OnDayStart; // Called when a new day starts
            helper.Events.Player.Warped += OnWarped; // Called when the player warps to a new location
            this.Helper.Events.Content.AssetRequested += OnAssetRequested; // Called when an asset is requested
            helper.Events.GameLoop.Saving += OnSaving; // Flush pending writes
            helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle; // Flush pending writes
            helper.ConsoleCommands.Add(
                name: "roosteraddon_dump_state",
                documentation: "Write a JSON snapshot of RoosterAddon state to the mod data folder.",
                callback: DumpStateCommand
            );

            // List of textures to be loaded
            var TexturesList = new List<Textures>
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

            // Loop through each texture and add it to the ModTextures dictionary
            foreach (var item in TexturesList)
            {
                ModTextures.Add(
                    item,
                    new Dictionary<TextureDetails, string>
                    {
                        {
                            TextureDetails.GamePath,
                            "Mods/"
                                + ModManifest.UniqueID
                                + "/"
                                + ModConfig.GetEnumName(item).ToLower()
                        }
                    }
                );
                ModTextures[item]
                    .Add(
                        TextureDetails.MoldFolderPath,
                        "assets/" + ModConfig.GetEnumName(item).ToLower() + ".png"
                    );
            }
            RoosterHelper = new RoosterControl(ref ModCfg, this.Helper, this.Monitor);
        }

        // Event handler for when the player warps to a new location
        private void OnWarped(object? sender, WarpedEventArgs e)
        {
            // Check if the update rate option is set to update on location change
            if (
                ModCfg.Options[OptionEntries.UpdateRate][OptionDetails.Value]
                == OptionValues.OnLocationChange
            )
            {
                // Invalidate the cache for farm animals data
                _ = this.Helper.GameContent.InvalidateCache("Data/FarmAnimals");
            }
        }

        private void OnLocaleChanged(object? sender, LocaleChangedEventArgs e)
        {
            //translation.setUpModTranslation(Helper);
        }

        /// <summary>The method called after a new day starts.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        // Event handler for when a new day starts
        private void OnDayStart(object? sender, DayStartedEventArgs e)
        {
            // Set flag to indicate that the day has started
            HasDayStarted = true;

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

            // Parse the raw conditions into managed conditions using ContentPatcher
            conditionSuperUnlucky = new ManagedConditions(ContentPatcher!.ParseConditions(
                manifest: ModManifest,
                rawConditions: rawSuperUnlucky,
                formatVersion: new SemanticVersion("2.0.0")
            ));
            conditionUnlucky = new ManagedConditions(ContentPatcher.ParseConditions(
                manifest: ModManifest,
                rawConditions: rawUnlucky,
                formatVersion: new SemanticVersion("2.0.0")
            ));
            conditionNeutral = new ManagedConditions(ContentPatcher.ParseConditions(
                manifest: ModManifest,
                rawConditions: rawNeutral,
                formatVersion: new SemanticVersion("2.0.0")
            ));
            conditionLucky = new ManagedConditions(ContentPatcher.ParseConditions(
                manifest: ModManifest,
                rawConditions: rawLucky,
                formatVersion: new SemanticVersion("2.0.0")
            ));
            conditionSuperLucky = new ManagedConditions(ContentPatcher.ParseConditions(
                manifest: ModManifest,
                rawConditions: rawSuperLucky,
                formatVersion: new SemanticVersion("2.0.0")
            ));
            conditionStuckInsideSeason = new ManagedConditions(ContentPatcher.ParseConditions(
                manifest: ModManifest,
                rawConditions: rawStuckInsideSeason,
                formatVersion: new SemanticVersion("2.0.0")
            ));
            conditionStuckInsideWeather = new ManagedConditions(ContentPatcher.ParseConditions(
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
                _ = this.Helper.GameContent.InvalidateCache("Data/FarmAnimals");
            }
            // Get the hatch only by fertilized option
            // Create a new RoosterControl instance
            RoosterHelper.DailyLuck = DailyLuck;
        }

        // Commented out code to invalidate the cache for farm animals data
        //bool a= this.Helper.GameContent.InvalidateCache
        private const string FarmAnimalsAssetName = "Data/FarmAnimals";
        private const string ObjectsAssetName = "Data/Objects";

        private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
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
                    RoosterHelper.SetUpRoosters();
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
                    RequestObjectsCacheInvalidation();

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
            // Initialize a list to store valid animal keys
            var validAnimalKeys = new List<string>();

            // Loop through each rooster and its females
            foreach (var rooster in RoosterHelper.Roosters.Select(r => r.Value))
            {
                // Add the rooster and its females to the valid animal keys list
                validAnimalKeys.Add(rooster.FarmAnimalName);
                validAnimalKeys.Add(rooster.Female);
                rooster.Females.ForEach(female => validAnimalKeys.Add(female));
            }

            // Filter the farm animals data to only include valid animal keys
            var FarmAnimals = data.Where(a => validAnimalKeys.Contains(a.Key))
                .ToDictionary(a => a.Key, a => a.Value);

            // Create a new RoosterDataExport instance
            var model = new RoosterDataExport(RoosterHelper.Roosters);

            // Save the rooster data to a JSON file
            this.Helper.Data.WriteJsonFile(
                $"data/{StardewModdingAPI.Constants.SaveFolderName}.json",
                model
            );

            // Create a new FarmAnimalsExport instance
            var FarmAnimalsToExport = new FarmAnimalsExport(FarmAnimals);

            // Save the farm animals data to a JSON file
            this.Helper.Data.WriteJsonFile(
                $"data/{StardewModdingAPI.Constants.SaveFolderName}-farmAnimals.json",
                FarmAnimalsToExport
            );

            // Check if debug logging is enabled
            if (Convert.ToBoolean(ModCfg.Options[OptionEntries.EnableDebug][OptionDetails.Value]))
            {
                // Log the daily luck, rooster data,and farm animals data
                Logger.LogDailyLuck(DailyLuck, Monitor);
                Logger.LogRoosterData(RoosterHelper.Roosters, Monitor);
                Logger.LogFarmAnimals(FarmAnimals, Monitor);
            }
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

                    // BetterTruffles tagging (single pass)
                    var betterTrufflesIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    foreach (var rooster in RoosterHelper.Roosters.Values)
                    {
                        var defaultProduce = rooster.AllProduces[ProduceTypeEntries.Produce][ProduceEntries.First]
                            .GetValueOrDefault(RoosterItemDataEntries.Name, "");
                        var deluxeDefaultProduce = rooster.AllProduces[ProduceTypeEntries.Deluxe][ProduceEntries.First]
                            .GetValueOrDefault(RoosterItemDataEntries.Name, "");

                        if (!string.IsNullOrWhiteSpace(defaultProduce))
                            betterTrufflesIds.Add(defaultProduce);

                        if (!string.IsNullOrWhiteSpace(deluxeDefaultProduce))
                            betterTrufflesIds.Add(deluxeDefaultProduce);
                    }

                    foreach (var objectId in betterTrufflesIds)
                    {
                        if (!dataObject.TryGetValue(objectId, out var obj))
                            continue;

                        obj.ContextTags ??= new();
                        if (!obj.ContextTags.Contains("temisthem_bettertruffles"))
                            obj.ContextTags.Add("temisthem_bettertruffles");
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
            return hatchApi != null && hatchApi.CloneEggs;
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

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            ContentPatcher = this.Helper.ModRegistry.GetApi<IContentPatcherAPI>(
                "Pathoschild.ContentPatcher"
            )!;
            hatchApi = Helper.ModRegistry.GetApi<ILetMeHatchApi>("Mikew.letmehatchthisegg.smapi")!;

            TryRegisterGmcm();
        }

        /// <summary>
        /// Handles the UpdateTicked event.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            if (_nextTickActions.Count == 0)
            {
                Helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;
                _isNextTickHandlerSubscribed = false;
                return;
            }

            while (_nextTickActions.Count > 0)
            {
                var action = _nextTickActions.Dequeue();
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    Monitor.Log($"Scheduled action failed: {ex}", LogLevel.Warn);
                }
            }

            Helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;
            _isNextTickHandlerSubscribed = false;
        }

        
        private void ScheduleNextTick(Action action)
        {
            _nextTickActions.Enqueue(action);

            if (_isNextTickHandlerSubscribed)
                return;

            _isNextTickHandlerSubscribed = true;
            Helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        }

        private void EnsureExclusionsLoaded()
        {
            var exclusions = Helper.Data.ReadJsonFile<AnimalExclusion>("exclusions.json");
            if (exclusions is null)
            {
                exclusions = new AnimalExclusion();
                Helper.Data.WriteJsonFile("exclusions.json", exclusions);
            }

            _animalExclusions = exclusions;
        }

        private void EnsureRoosterFemalesLoaded()
        {
            var females = Helper.Data.ReadJsonFile<RoosterFemales>("roosterFemales.json");
            if (females is null)
            {
                females = new RoosterFemales();
                Helper.Data.WriteJsonFile("roosterFemales.json", females);
            }

            _roosterFemales = females;
            ModCfg.FemalesOptions = _roosterFemales.Females;
        }

        private void TryRegisterGmcm()
        {
            configMenu = Helper.ModRegistry.GetApi<IGenericModConfigMenuAPI>(
                "spacechase0.GenericModConfigMenu"
            )!;

            if (configMenu is null)
                return;

            configMenu.Unregister(ModManifest);

            configMenu.Register(
                mod: ModManifest,
                reset: () =>
                {
                    ModCfg = new ModConfig();
                    Helper.WriteConfig(ModCfg);
                    ModCfg.FemalesOptions = _roosterFemales.Females;
                },
                save: () =>
                {
                    Helper.WriteConfig(ModCfg);

                    _roosterFemales.Females = ModCfg.FemalesOptions;
                    Helper.Data.WriteJsonFile("roosterFemales.json", _roosterFemales);

                    if (Context.IsWorldReady && RoosterHelper.Roosters != null)
                    {
                        foreach (var rooster in RoosterHelper.Roosters.Values)
                        {
                            rooster.Females = _roosterFemales
                                .Females.Where(f =>
                                    f.Value.Equals(rooster.FarmAnimalName, StringComparison.OrdinalIgnoreCase)
                                )
                                .Select(f => f.Key)
                                .ToList();
                        }

                        var model = new RoosterDataExport(RoosterHelper.Roosters);
                        Helper.Data.WriteJsonFile(
                            $"data/{StardewModdingAPI.Constants.SaveFolderName}.json",
                            model
                        );
                    }
                }
            );

            ModCfg.FemalesOptions = _roosterFemales.Females;
            GMCMSetup(ref configMenu);
        }

        private void MarkRoosterFemalesDirty()
        {
            _roosterFemalesDirty = true;

            if (_roosterFemalesSaveScheduled)
                return;

            _roosterFemalesSaveScheduled = true;
            ScheduleNextTick(() =>
            {
                _roosterFemalesSaveScheduled = false;
                PersistRoosterFemalesIfDirty();
            });
        }

        private void PersistRoosterFemalesIfDirty()
        {
            if (!_roosterFemalesDirty)
                return;

            Helper.Data.WriteJsonFile("roosterFemales.json", _roosterFemales);
            _roosterFemalesDirty = false;
        }

        private void RequestObjectsCacheInvalidation()
        {
            if (_objectsCacheInvalidationScheduled)
                return;

            _objectsCacheInvalidationScheduled = true;
            ScheduleNextTick(() =>
            {
                _objectsCacheInvalidationScheduled = false;
            });
        }

        private void OnSaving(object? sender, SavingEventArgs e)
        {
            PersistRoosterFemalesIfDirty();
        }

        private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
        {
            PersistRoosterFemalesIfDirty();
        }

        private void DumpStateCommand(string command, string[] args)
        {
            var stamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var path = $"state-dumps/roosteraddon_state_{stamp}.json";

            var dump = new RoosterAddonStateDump
            {
                GeneratedAtUtc = DateTime.UtcNow,
                ModVersion = ModManifest.Version.ToString(),
                WorldReady = Context.IsWorldReady,
                SaveFolderName = StardewModdingAPI.Constants.SaveFolderName,
                DailyLuck = Context.IsWorldReady ? Game1.player?.DailyLuck : null,
                Config = ModCfg,
                RoosterFemales = _roosterFemales,
                Exclusions = _animalExclusions,
                Roosters = RoosterHelper?.Roosters?.Values.Select(r => new RoosterAddonRoosterDump
                {
                    Type = r.Type.ToString(),
                    FarmAnimalName = r.FarmAnimalName,
                    Female = r.Female,
                    Females = r.Females?.ToList() ?? new List<string>(),
                    Eggs = r.Eggs?.ToDictionary(
                        kvp => kvp.Key.ToString(),
                        kvp => kvp.Value.ToDictionary(x => x.Key, x => x.Value)
                    ) ?? new Dictionary<string, Dictionary<string, string>>(),
                    ProducesSummary = r.AllProduces?.ToDictionary(
                        kvp => kvp.Key.ToString(),
                        kvp => kvp.Value.ToDictionary(
                            pe => pe.Key.ToString(),
                            pe => pe.Value.ContainsKey(RoosterItemDataEntries.Name)
                                ? pe.Value[RoosterItemDataEntries.Name]
                                : ""
                        )
                    ) ?? new Dictionary<string, Dictionary<string, string>>()
                }).ToList() ?? new List<RoosterAddonRoosterDump>(),
                ObjectsPatchSummary = BuildObjectsPatchSummary()
            };

            Helper.Data.WriteJsonFile(path, dump);
            Monitor.Log($"Wrote state dump: {path}", LogLevel.Info);
        }

        private RoosterAddonObjectsPatchSummary BuildObjectsPatchSummary()
        {
            var betterTrufflesIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (RoosterHelper?.Roosters != null)
            {
                foreach (var rooster in RoosterHelper.Roosters.Values)
                {
                    var defaultProduce = rooster.AllProduces[ProduceTypeEntries.Produce][ProduceEntries.First]
                        .GetValueOrDefault(RoosterItemDataEntries.Name, "");
                    var deluxeDefaultProduce = rooster.AllProduces[ProduceTypeEntries.Deluxe][ProduceEntries.First]
                        .GetValueOrDefault(RoosterItemDataEntries.Name, "");

                    if (!string.IsNullOrWhiteSpace(defaultProduce))
                        betterTrufflesIds.Add(defaultProduce);

                    if (!string.IsNullOrWhiteSpace(deluxeDefaultProduce))
                        betterTrufflesIds.Add(deluxeDefaultProduce);
                }
            }

            return new RoosterAddonObjectsPatchSummary
            {
                BetterTrufflesTaggedObjectIds = betterTrufflesIds.OrderBy(x => x).ToList(),
                FemaleEggs = RoosterHelper?.FemaleEggs?.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.ToDictionary(x => x.Key, x => x.Value)
                ) ?? new Dictionary<string, Dictionary<string, string>>()
            };
        }

        
        private sealed class KeyValuePairIgnoreCaseComparer : IEqualityComparer<KeyValuePair<string, string>>
        {
            public bool Equals(KeyValuePair<string, string> x, KeyValuePair<string, string> y)
            {
                return string.Equals(x.Key, y.Key, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(x.Value, y.Value, StringComparison.OrdinalIgnoreCase);
            }

            public int GetHashCode(KeyValuePair<string, string> obj)
            {
                return HashCode.Combine(
                    obj.Key?.ToLowerInvariant(),
                    obj.Value?.ToLowerInvariant()
                );
            }
        }

private sealed class RoosterAddonStateDump
        {
            public DateTime GeneratedAtUtc { get; set; }
            public string ModVersion { get; set; } = "";
            public bool WorldReady { get; set; }
            public string? SaveFolderName { get; set; }
            public double? DailyLuck { get; set; }
            public ModConfig Config { get; set; } = new();
            public RoosterFemales RoosterFemales { get; set; } = new();
            public AnimalExclusion Exclusions { get; set; } = new();
            public List<RoosterAddonRoosterDump> Roosters { get; set; } = new();
            public RoosterAddonObjectsPatchSummary ObjectsPatchSummary { get; set; } = new();
        }

        private sealed class RoosterAddonRoosterDump
        {
            public string Type { get; set; } = "";
            public string FarmAnimalName { get; set; } = "";
            public string Female { get; set; } = "";
            public List<string> Females { get; set; } = new();
            public Dictionary<string, Dictionary<string, string>> Eggs { get; set; } = new();
            public Dictionary<string, Dictionary<string, string>> ProducesSummary { get; set; } = new();
        }

        private sealed class RoosterAddonObjectsPatchSummary
        {
            public List<string> BetterTrufflesTaggedObjectIds { get; set; } = new();
            public Dictionary<string, Dictionary<string, string>> FemaleEggs { get; set; } = new();
        }

internal void GetRoostersFemales(
            IDictionary<string, FarmAnimalData> animalData,
            Dictionary<RoosterType, RoosterData> Roosters
        )
        {
            var animalExclusions = _animalExclusions;
            _roosterFemales ??= new RoosterFemales();

            var beforeNormalized = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in _roosterFemales.Females)
                beforeNormalized[kvp.Key] = kvp.Value;

            var eggTypes = Roosters
                .Values.SelectMany(rd => rd.Eggs.Values)
                .Where(ed => ed.ContainsKey(RoosterItemDataEntries.Name))
                .Select(ed => ed[RoosterItemDataEntries.Name])
                .ToList();

            var defaultFemales = Roosters.Values.ToDictionary(x => x.Female, x => x.FarmAnimalName);
            var foundFemales = defaultFemales.Keys.ToList();

            if (GetHatchApiCloneEggsOption())
            {
                foundFemales.AddRange(
                    GetFemales(animalData, eggTypes, animalExclusions).OrderBy(f => f)
                );
            }

            foreach (var female in _roosterFemales.Females.Keys.Where(f => !foundFemales.Contains(f)).ToList())
            {
                _roosterFemales.Females.Remove(female);
            }

            foreach (var female in foundFemales)
            {
                if (!_roosterFemales.Females.ContainsKey(female))
                {
                    _roosterFemales.Females[female] = defaultFemales.ContainsKey(female)
                        ? defaultFemales[female]
                        : OptionValues.Off;
                }
            }

            var afterNormalized = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in _roosterFemales.Females)
                afterNormalized[kvp.Key] = kvp.Value;

            if (!beforeNormalized.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase)
                .SequenceEqual(afterNormalized.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase),
                    new KeyValuePairIgnoreCaseComparer()))
            {
                MarkRoosterFemalesDirty();
            }

            ModCfg.FemalesOptions = _roosterFemales.Females;

            foreach (var roosterType in Roosters.Keys)
            {
                RoosterHelper.Roosters[roosterType].Females = _roosterFemales
                    .Females.Where(f =>
                        f.Value.Equals(Roosters[roosterType].FarmAnimalName, StringComparison.OrdinalIgnoreCase)
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
                    && hatchApi.AnimalsWithClonedEggs.Contains(a.Key)
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
