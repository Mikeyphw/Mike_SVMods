using System.Collections.Generic;
using RoosterControl.Application.Hatching.Decisions;
using RoosterControl.Constants;
using RoosterControl.Enums;
using StardewValley.GameData.FarmAnimals;

namespace RoosterControl.Application.Hatching;

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

        if (!TryGetChance(rooster, EggTypeEntries.Fertilized, out var fertilizedChance))
            fertilizedChance = 0;

        if (!TryGetEggId(rooster, EggTypeEntries.Fertilized, out var roosterFertilizedEggId))
            return;

        if (!ctx.RoosterHelper.FemaleEggs.TryGetValue(femaleKey, out var femaleEggs)
            || !femaleEggs.TryGetValue(EggTypeEntries.Fertilized, out var femaleFertilizedEggId))
        {
            return;
        }

        var decision = HatchingDecisionEngine.DecideFertilizedOnly(
            new FertilizedOnlyRule(
                FemaleUpgradeChancePercent: fertilizedChance,
                RoosterToChickenRatioPercent: ctx.RoosterToChickenRatio,
                RoosterFertilizedEggId: roosterFertilizedEggId,
                FemaleFertilizedEggId: femaleFertilizedEggId
            ),
            ctx.Roller
        );

        if (decision.UpgradeFemaleProduceToFertilized)
        {
            data[femaleKey].DeluxeProduceItemIds = new List<FarmAnimalProduce> { new() { ItemId = femaleFertilizedEggId } };
            data[femaleKey].ProduceItemIds = new List<FarmAnimalProduce> { new() { ItemId = femaleFertilizedEggId } };
        }

        // clear egg ids across all affected animals
        data[rooster.FarmAnimalName].EggItemIds = null;
        data[femaleKey].EggItemIds = null;
        foreach (var otherAnimal in otherAnimalsSharingEggs)
        {
            if (data.ContainsKey(otherAnimal))
                data[otherAnimal].EggItemIds = null;
        }

        if (decision.RoosterHatchesFertilizedEgg && !string.IsNullOrWhiteSpace(decision.RoosterEggId))
        {
            data[rooster.FarmAnimalName].EggItemIds ??= new();
            data[rooster.FarmAnimalName].EggItemIds.Add(decision.RoosterEggId);
            return;
        }

        if (!decision.RoosterHatchesFertilizedEgg && !string.IsNullOrWhiteSpace(decision.FemaleEggId))
            data[femaleKey].EggItemIds = new List<string> { decision.FemaleEggId };
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
        if (!egg.TryGetValue(RoosterItemDataEntries.Name, out var rawEggId))
            return false;

        eggId = rawEggId ?? string.Empty;
        return !string.IsNullOrWhiteSpace(eggId);
    }
}
