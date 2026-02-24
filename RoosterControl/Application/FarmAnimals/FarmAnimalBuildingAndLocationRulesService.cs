using System;
using RoosterControl.Enums;
using RoosterControl.Framework.Config;
using StardewValley.GameData.FarmAnimals;

namespace RoosterControl.Application.FarmAnimals;

/// <summary>
/// Applies building requirements and location-related rules to FarmAnimals entries.
/// </summary>
internal sealed class FarmAnimalBuildingAndLocationRulesService
{
    public void Apply(
        RoosterData rooster,
        FarmAnimalData entry,
        bool enableStuckInside,
        bool isStuckInside
    )
    {
        if (rooster is null) throw new ArgumentNullException(nameof(rooster));
        if (entry is null) throw new ArgumentNullException(nameof(entry));

        entry.RequiredBuilding = ToGameBuildingKey(rooster.BuildingRequired);
        entry.DaysToMature = rooster.MatureDays;
        entry.DaysToProduce = rooster.ProduceDays;

        var chosenHarvestType =
            rooster.HarvestType == HarvestType.DigUp
                ? FarmAnimalHarvestType.DigUp
                : FarmAnimalHarvestType.DropOvernight;

        entry.HarvestType = enableStuckInside && isStuckInside
            ? FarmAnimalHarvestType.DropOvernight
            : chosenHarvestType;
    }

    private static string ToGameBuildingKey(RequiredBuilding building)
        => building switch
        {
            RequiredBuilding.Coop => "Coop",
            RequiredBuilding.BigCoop => "Big Coop",
            RequiredBuilding.DeluxeCoop => "Deluxe Coop",
            _ => "Big Coop",
        };
}
