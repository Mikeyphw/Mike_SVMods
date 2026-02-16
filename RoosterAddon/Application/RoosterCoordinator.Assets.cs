using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using RoosterAddon.Constants;
using RoosterAddon.Enums;
using RoosterAddon.Helpers;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley.GameData.FarmAnimals;
using StardewValley.GameData.Objects;

namespace RoosterAddon.Application;

internal sealed partial class RoosterCoordinator
{
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

}
