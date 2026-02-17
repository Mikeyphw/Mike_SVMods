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

            // Produce rules (including BlueEgg override) are handled by a service.
            _produceRulesService.ApplyProduceEdits(
                rooster,
                data,
                Config,
                RoosterHelper,
                blueEggModInstalled: HasMod("6480.blueegg", Helper)
            );

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

private bool GetEnableStuckInsideOption()
        {
            return Config.General.EnableStuckInside;
        }
}
