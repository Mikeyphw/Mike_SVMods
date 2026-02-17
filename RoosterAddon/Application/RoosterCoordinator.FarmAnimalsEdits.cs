using System;
using System.Collections.Generic;
using RoosterAddon.Constants;
using RoosterAddon.Enums;
using RoosterAddon.Helpers;
using StardewValley.GameData.FarmAnimals;

namespace RoosterAddon.Application;

internal sealed partial class RoosterCoordinator
{
    private static string ToGameBuildingKey(RequiredBuilding building)
        => building switch
        {
            RequiredBuilding.Coop => "Coop",
            RequiredBuilding.BigCoop => "Big Coop",
            RequiredBuilding.DeluxeCoop => "Deluxe Coop",
            _ => "Big Coop",
        };

private void SetRoosterData(RoosterData rooster, IDictionary<string, FarmAnimalData> data)
        {
            // Set the required building, harvest type, days to mature, and days to produce
            data[rooster.FarmAnimalName].RequiredBuilding = ToGameBuildingKey(rooster.BuildingRequired);
            data[rooster.FarmAnimalName].HarvestType =
                rooster.HarvestType == HarvestType.DigUp
                    ? FarmAnimalHarvestType.DigUp
                    : FarmAnimalHarvestType.DropOvernight;
            data[rooster.FarmAnimalName].DaysToMature = rooster.MatureDays;
            data[rooster.FarmAnimalName].DaysToProduce = rooster.ProduceDays;

            // Handle the blue egg logic if the mod "6480.blueegg" is present and the rooster is blue
            if (HasMod("6480.blueegg", Helper) && rooster.FarmAnimalName == RoosterKeys.Blue)
            {
                // Get the new blue egg translation
                var newBlueEgg = RoosterControl.ProduceTranslation(Config.Roosters[RoosterType.Blue].BlueEggChoice);

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
                    && Config.Roosters[RoosterType.Blue].BypassBlueConditions
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
                    rooster.HarvestType == HarvestType.DigUp
                        ? FarmAnimalHarvestType.DigUp
                        : FarmAnimalHarvestType.DropOvernight;

                // Set the harvest type for the rooster based on the stuck inside option
                data[rooster.FarmAnimalName].HarvestType = isStuckInside
                    ? FarmAnimalHarvestType.DropOvernight
                    : chosenHarvestType;
            }
        }

private void ProduceRandomizationSystem(
            RoosterData rooster,
            IDictionary<string, FarmAnimalData> data
        )
        {
            // Handle the logic for rooster produce options
            if (rooster.ProduceOption != ProduceMode.KeepOriginalProduce)
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
            if (rooster.DeluxeProduceOption == DeluxeProduceMode.CustomizeBelow)
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
            else if (rooster.DeluxeProduceOption == DeluxeProduceMode.Off)
            {
                data[rooster.FarmAnimalName].DeluxeProduceItemIds = null;
            }
        }

private bool GetEnableStuckInsideOption()
        {
            return Config.General.EnableStuckInside;
        }
}
