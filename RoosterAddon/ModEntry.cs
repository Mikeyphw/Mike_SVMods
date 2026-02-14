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
        ContentPatcher.IManagedConditions? conditionSuperUnlucky, // Condition for super unlucky state
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

            // Subscribe to game events
            helper.Events.GameLoop.GameLaunched += OnGameLaunched; // Called when the game is launched
            this.Helper.Events.GameLoop.UpdateTicked += OnUpdateTicked; // Called every game tick
            this.Helper.Events.GameLoop.DayStarted += OnDayStart; // Called when a new day starts
            helper.Events.Player.Warped += OnWarped; // Called when the player warps to a new location
            this.Helper.Events.Content.AssetRequested += OnAssetRequested; // Called when an asset is requested

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
            conditionSuperUnlucky = ContentPatcher!.ParseConditions(
                manifest: ModManifest,
                rawConditions: rawSuperUnlucky,
                formatVersion: new SemanticVersion("2.0.0")
            );
            conditionUnlucky = ContentPatcher.ParseConditions(
                manifest: ModManifest,
                rawConditions: rawUnlucky,
                formatVersion: new SemanticVersion("2.0.0")
            );
            conditionNeutral = ContentPatcher.ParseConditions(
                manifest: ModManifest,
                rawConditions: rawNeutral,
                formatVersion: new SemanticVersion("2.0.0")
            );
            conditionLucky = ContentPatcher.ParseConditions(
                manifest: ModManifest,
                rawConditions: rawLucky,
                formatVersion: new SemanticVersion("2.0.0")
            );
            conditionSuperLucky = ContentPatcher.ParseConditions(
                manifest: ModManifest,
                rawConditions: rawSuperLucky,
                formatVersion: new SemanticVersion("2.0.0")
            );
            conditionStuckInsideSeason = ContentPatcher.ParseConditions(
                manifest: ModManifest,
                rawConditions: rawStuckInsideSeason,
                formatVersion: new SemanticVersion("2.0.0")
            );
            conditionStuckInsideWeather = ContentPatcher.ParseConditions(
                manifest: ModManifest,
                rawConditions: rawStuckInsideWeather,
                formatVersion: new SemanticVersion("2.0.0")
            );

            // Update the context for each condition
            _ = conditionSuperUnlucky.UpdateContext();
            _ = conditionUnlucky.UpdateContext();
            _ = conditionNeutral.UpdateContext();
            _ = conditionLucky.UpdateContext();
            _ = conditionSuperLucky.UpdateContext();
            _ = conditionStuckInsideSeason.UpdateContext();
            _ = conditionStuckInsideWeather.UpdateContext();

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

                    // Loop through each rooster type and its produces
                    foreach (
                        var roosterType in RoosterHelper.Roosters.Select(data =>
                            data.Value.AllProduces
                        )
                    )
                    {
                        // Get the default produce and deluxe default produce for the rooster type
                        var defaultProduce = roosterType[ProduceTypeEntries.Produce][
                            ProduceEntries.First
                        ][RoosterItemDataEntries.Name];
                        var deluxeDefaultProduce = roosterType[ProduceTypeEntries.Deluxe][
                            ProduceEntries.First
                        ][RoosterItemDataEntries.Name];

                        // Loop through each game object in the data object
                        foreach (var gameObject in dataObject)
                        {
                            // Check if the game object key matches the default produce or deluxe default produce
                            if (
                                gameObject.Key == defaultProduce
                                || gameObject.Key == deluxeDefaultProduce
                            )
                            {
                                // Ensure the ContextTags is not null and add "temisthem_bettertruffles" to it
                                gameObject.Value.ContextTags ??= new();
                                gameObject.Value.ContextTags.Add("temisthem_bettertruffles");
                            }
                        }
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
            Helper.GameContent.InvalidateCache("Data/Objects");
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
                ) && RoosterControl.UpdatePercentage(female.Length)
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
                && RoosterControl.UpdatePercentage(female.Length)
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
                && RoosterControl.UpdatePercentage(female.Length)
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
        }

        /// <summary>
        /// Handles the UpdateTicked event.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            // Unsubscribe from the UpdateTicked event to prevent multiple registrations
            // Get the Generic Mod Config Menu API (if it's installed)
            configMenu = Helper.ModRegistry.GetApi<IGenericModConfigMenuAPI>(
                "spacechase0.GenericModConfigMenu"
            )!;

            // If the API is not available, exit the method
            if (configMenu is null)
                return;

            // Unregister the mod from the config menu
            configMenu.Unregister(ModManifest);

            // Register the mod with the config menu
            configMenu.Register(
                mod: ModManifest,
                reset: () =>
                {
                    ModCfg = new ModConfig();
                    Helper.WriteConfig<ModConfig>(ModCfg);
                    ModCfg.FemalesOptions = _roosterFemales.Females;
                },
                save: () =>
                {
                    Helper.WriteConfig(ModCfg);
                    _roosterFemales.Females = ModCfg.FemalesOptions;
                    Helper.Data.WriteJsonFile("roosterFemales.json", _roosterFemales);
                    if (RoosterHelper.Roosters != null)
                    {
                        foreach (var rooster in RoosterHelper.Roosters.Values)
                        {
                            rooster.Females = _roosterFemales!
                                .Females.Where(f =>
                                    f.Value.ToLower() == rooster.FarmAnimalName.ToLower()
                                )
                                .Select(f => f.Key)
                                .ToList();
                        }
                        var model = new RoosterDataExport(RoosterHelper.Roosters);
                        // Save the rooster data to a JSON file
                        this.Helper.Data.WriteJsonFile(
                            $"data/{StardewModdingAPI.Constants.SaveFolderName}.json",
                            model
                        );
                    }
                }
            );
            ModCfg.FemalesOptions = _roosterFemales.Females;
            // Set up the GMCM (Generic Mod Config Menu) for this mod
            GMCMSetup(ref configMenu);
        }

        internal void GetRoostersFemales(
            IDictionary<string, FarmAnimalData> animalData,
            Dictionary<RoosterType, RoosterData> Roosters
        )
        {
            var animalExclusions = GetExclusions();
            _roosterFemales = Helper.Data.ReadJsonFile<RoosterFemales>("roosterFemales.json");
            if (_roosterFemales == null)
            {
                Helper.Data.WriteJsonFile("roosterFemales.json", new RoosterFemales());
                _roosterFemales = Helper.Data.ReadJsonFile<RoosterFemales>("roosterFemales.json");
            }
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
            foreach (
                var female in _roosterFemales!.Females.Keys.Where(f => !foundFemales.Contains(f))
            )
            {
                _roosterFemales.Females.Remove(female);
            }
            foreach (var female in foundFemales)
            {
                if (!_roosterFemales!.Females.ContainsKey(female))
                {
                    _roosterFemales!.Females[female] = defaultFemales.ContainsKey(female)
                        ? defaultFemales[female]
                        : OptionValues.Off;
                }
            }
            Helper.Data.WriteJsonFile("roosterFemales.json", _roosterFemales);
            foreach (var roosterType in Roosters.Keys)
            {
                RoosterHelper.Roosters[roosterType].Females = _roosterFemales!
                    .Females.Where(f =>
                        f.Value.ToLower() == Roosters[roosterType].FarmAnimalName.ToLower()
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
            var exclusions = Helper.Data.ReadJsonFile<AnimalExclusion>("exclusions.json");
            if (exclusions == null)
            {
                Helper.Data.WriteJsonFile("exclusions.json", new AnimalExclusion());
                exclusions = Helper.Data.ReadJsonFile<AnimalExclusion>("exclusions.json");
            }

            var eggListExclusionsModID = exclusions!.AnimalExclusionsModID;

            var animalExclusions = new List<string>();
            foreach (var animal in exclusions.AnimalExclusions)
            {
                animalExclusions.AddRange(GetExclusions(animal));
            }
            animalExclusions = animalExclusions.Distinct().ToList(); // Remove duplicates
            return new AnimalExclusion
            {
                AnimalExclusions = animalExclusions,
                AnimalExclusionsModID = eggListExclusionsModID
            };
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
