using System;
using System.Collections.Generic;
using System.Linq;
using Mikew.BirdsOfAFeather.Framework;
using Mikew.BirdsOfAFeather.Helpers;
using Microsoft.Xna.Framework;
using RoosterAddon.Constants;
using RoosterAddon.Enums;
using RoosterAddon.Helpers;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.GameData.FarmAnimals;
using StardewValley.GameData.Objects;

using ILetMeHatchApi = Mikew.BirdsOfAFeather.Framework.ILetMeHatchApi;
using LuckyTypes = Mikew.BirdsOfAFeather.EnumsAndStrings.Enums.LuckyTypes;

namespace RoosterAddon.Application;

internal sealed partial class RoosterCoordinator
{
        public void OnDayStarted(object? sender, StardewModdingAPI.Events.DayStartedEventArgs e) => OnDayStart(sender, e);


        /// <summary>The method called after a new day starts.</summary>
        private void OnDayStart(object? sender, DayStartedEventArgs e)
        {
            HasDayStarted = true;

            EnsureRoostersSetup();

            DailyLuck = DailyLuckHelper.GetDailyLuck();
            RoosterHelper.DailyLuck = DailyLuck;

            // Evaluate "stuck inside" using CP when available, otherwise fall back to vanilla state.
            isStuckInside = false;
            if (ModEntry.ContentPatcher is not null)
            {
                var rawStuckInsideSeason = new Dictionary<string, string?>
                {
                    ["Season |contains=winter"] = "true",
                };
                var rawStuckInsideWeather = new Dictionary<string, string?>
                {
                    ["Weather |contains=rain, storm, snow, greenrain"] = "true",
                };

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

                conditionStuckInsideSeason.UpdateContext();
                conditionStuckInsideWeather.UpdateContext();
                isStuckInside = conditionStuckInsideSeason.IsMatch || conditionStuckInsideWeather.IsMatch;
            }
            else
            {
                isStuckInside =
                    string.Equals(Game1.currentSeason, "winter", StringComparison.OrdinalIgnoreCase)
                    || Game1.isRaining
                    || Game1.isLightning
                    || Game1.isSnowing;
            }

            if (ModCfg.Options[OptionEntries.UpdateRate][OptionDetails.Value] == OptionValues.OnDayStart)
                _ = Helper.GameContent.InvalidateCache(FarmAnimalsAssetName);
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

        private bool GetHatchOnlyByFertilizedOption()
        {
            return ModCfg.Options[OptionEntries.HatchingMode][OptionDetails.Value]
                != OptionValues.Original;
        }


        private bool GetHatchApiCloneEggsOption()
        {
            return HatchApi is not null && HatchApi.GetClonedAnimalsInfo().Count > 0;
        }


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

            var clonedFemales = new HashSet<string>(
                HatchApi.GetClonedAnimalsInfo().Select(t => t.FemaleKey),
                StringComparer.OrdinalIgnoreCase
            );

            return animalData
                .Where(a =>
                    clonedFemales.Contains(a.Key)
                    && a.Value.CustomFields != null
                    && a.Value.CustomFields.ContainsKey("ClonedEggs")
                    && GetClonedEggsList(a.Value.CustomFields["ClonedEggs"])
                        .Any(egg => eggTypes.Contains(egg, StringComparer.OrdinalIgnoreCase))
                    && !animalExclusions.AnimalExclusions.Contains(
                        a.Key,
                        StringComparer.OrdinalIgnoreCase
                    )
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
}
