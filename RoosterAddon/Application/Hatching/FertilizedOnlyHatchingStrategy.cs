using System;
using System.Collections.Generic;
using RoosterAddon.Constants;
using RoosterAddon.Enums;
using StardewValley.GameData.FarmAnimals;

namespace RoosterAddon.Application.Hatching;

internal sealed class FertilizedOnlyHatchingStrategy : IHatchingStrategy
{
    public void Apply(
        HatchingContext ctx,
        string femaleKey,
        RoosterData rooster,
        IDictionary<string, FarmAnimalData> data,
        IReadOnlyList<string> otherAnimalsSharingEggs
    )
    {
        if (!data.ContainsKey(rooster.FarmAnimalName) || !data.ContainsKey(femaleKey))
            return;

        // probabilistic upgrade: female produces fertilized eggs as regular + deluxe produce
        if (TryGetChance(rooster, EggTypeEntries.Fertilized, out var fertilizedChance)
            && HatchingContext.RollPercentage(fertilizedChance))
        {
            if (ctx.RoosterHelper.FemaleEggs.TryGetValue(femaleKey, out var femaleEggs)
                && femaleEggs.TryGetValue(EggTypeEntries.Fertilized, out var fertilizedEggId))
            {
                data[femaleKey].DeluxeProduceItemIds = new List<FarmAnimalProduce> { new() { ItemId = fertilizedEggId } };
                data[femaleKey].ProduceItemIds = new List<FarmAnimalProduce> { new() { ItemId = fertilizedEggId } };
            }
        }

        // clear egg ids across all affected animals
        data[rooster.FarmAnimalName].EggItemIds = null;
        data[femaleKey].EggItemIds = null;
        foreach (var otherAnimal in otherAnimalsSharingEggs)
        {
            if (data.ContainsKey(otherAnimal))
                data[otherAnimal].EggItemIds = null;
        }

        // rooster vs chicken ratio decides which animal hatches fertilized eggs
        if (TryGetEggId(rooster, EggTypeEntries.Fertilized, out var fertilizedEggName))
        {
            if (HatchingContext.RollPercentage(ctx.RoosterToChickenRatio))
            {
                data[rooster.FarmAnimalName].EggItemIds ??= new();
                data[rooster.FarmAnimalName].EggItemIds.Add(fertilizedEggName);
            }
            else if (ctx.RoosterHelper.FemaleEggs.TryGetValue(femaleKey, out var femaleEggs)
                     && femaleEggs.TryGetValue(EggTypeEntries.Fertilized, out var femaleFertilizedEggId))
            {
                data[femaleKey].EggItemIds = new List<string> { femaleFertilizedEggId };
            }
        }
    }

    private static bool TryGetChance(RoosterData rooster, EggTypeEntries eggType, out float chance)
    {
        chance = 0;
        if (!rooster.Eggs.TryGetValue(eggType, out var egg))
            return false;
        if (!egg.TryGetValue(RoosterItemDataEntries.Probability, out var raw))
            return false;
        return float.TryParse(raw, out chance);
    }

    private static bool TryGetEggId(RoosterData rooster, EggTypeEntries eggType, out string eggId)
    {
        eggId = string.Empty;
        if (!rooster.Eggs.TryGetValue(eggType, out var egg))
            return false;
        return egg.TryGetValue(RoosterItemDataEntries.Name, out eggId) && !string.IsNullOrWhiteSpace(eggId);
    }
}
