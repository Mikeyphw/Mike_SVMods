using System;
using System.Collections.Generic;
using RoosterControl.Constants;
using RoosterControl.Enums;
using RoosterControl.Framework.Config;
using RoosterControl.Helpers;
using StardewValley.GameData.FarmAnimals;

namespace RoosterControl.Application.FarmAnimals;

/// <summary>
/// Applies RoosterControl FarmAnimals "produce rules" edits (including BlueEgg override).
/// </summary>
internal sealed class FarmAnimalProduceRulesService
{
    public void ApplyProduceEdits(
        RoosterData rooster,
        IDictionary<string, FarmAnimalData> data,
        RoosterControlConfig config,
        RoosterHelper roosterHelper,
        bool blueEggModInstalled
    )
    {
        if (rooster is null) throw new ArgumentNullException(nameof(rooster));
        if (data is null) throw new ArgumentNullException(nameof(data));
        if (config is null) throw new ArgumentNullException(nameof(config));
        if (roosterHelper is null) throw new ArgumentNullException(nameof(roosterHelper));

        ApplyBlueEggOverrideIfNeeded(rooster, data, config, roosterHelper, blueEggModInstalled);
        ApplyProduceRandomization(rooster, data);
    }

    private static void ApplyBlueEggOverrideIfNeeded(
        RoosterData rooster,
        IDictionary<string, FarmAnimalData> data,
        RoosterControlConfig config,
        RoosterHelper roosterHelper,
        bool blueEggModInstalled
    )
    {
        if (!blueEggModInstalled)
            return;

        if (rooster.RoosterType != RoosterType.Blue)
            return;

        var newBlueEgg = roosterHelper.ProduceResolver.Resolve(config.Roosters[RoosterType.Blue].BlueEggChoice);

        roosterHelper.Roosters[RoosterType.Blue].Eggs[EggTypeEntries.Normal][RoosterItemDataEntries.Name] = newBlueEgg;

        if (!data.ContainsKey(rooster.Female))
            return;

        data[rooster.Female].ProduceItemIds = new List<FarmAnimalProduce>
        {
            new() { ItemId = newBlueEgg, Id = newBlueEgg }
        };
    }

    private static void ApplyProduceRandomization(RoosterData rooster, IDictionary<string, FarmAnimalData> data)
    {
        if (!data.ContainsKey(rooster.FarmAnimalName))
            return;

        if (rooster.ProduceOption != ProduceMode.KeepOriginalProduce)
        {
            var chosen = rooster.AllProduces[ProduceTypeEntries.Produce][ProduceEntries.Chosen][RoosterItemDataEntries.Name];
            data[rooster.FarmAnimalName].ProduceItemIds = new List<FarmAnimalProduce>
            {
                new() { ItemId = chosen, Id = chosen }
            };
        }

        if (rooster.DeluxeProduceOption == DeluxeProduceMode.CustomizeBelow)
        {
            var chosen = rooster.AllProduces[ProduceTypeEntries.Deluxe][ProduceEntries.Chosen][RoosterItemDataEntries.Name];
            data[rooster.FarmAnimalName].DeluxeProduceItemIds = new List<FarmAnimalProduce>
            {
                new() { ItemId = chosen, Id = chosen }
            };
        }
        else if (rooster.DeluxeProduceOption == DeluxeProduceMode.Off)
        {
            data[rooster.FarmAnimalName].DeluxeProduceItemIds = null;
        }
    }
}
