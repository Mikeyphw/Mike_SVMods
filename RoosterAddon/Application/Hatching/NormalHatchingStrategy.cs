using System;
using System.Collections.Generic;
using RoosterAddon.Constants;
using RoosterAddon.Enums;
using StardewValley.GameData.FarmAnimals;

namespace RoosterAddon.Application.Hatching;

internal sealed class NormalHatchingStrategy : IHatchingStrategy
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

        data[rooster.FarmAnimalName].EggItemIds ??= new();
        data[femaleKey].EggItemIds ??= new();

        ApplyEggChance(
            ctx,
            femaleKey,
            rooster,
            data,
            otherAnimalsSharingEggs,
            eggType: EggTypeEntries.Normal,
            enabled: ctx.EnableEggProbability
        );

        ApplyEggChance(
            ctx,
            femaleKey,
            rooster,
            data,
            otherAnimalsSharingEggs,
            eggType: EggTypeEntries.Large,
            enabled: ctx.EnableLargeEggProbability
        );
    }

    private static void ApplyEggChance(
        HatchingContext ctx,
        string femaleKey,
        RoosterData rooster,
        IDictionary<string, FarmAnimalData> data,
        IReadOnlyList<string> otherAnimalsSharingEggs,
        EggTypeEntries eggType,
        bool enabled
    )
    {
        if (!enabled)
            return;

        if (!rooster.Eggs.TryGetValue(eggType, out var egg) || !egg.TryGetValue(RoosterItemDataEntries.Probability, out var rawChance))
            return;

        if (!int.TryParse(rawChance, out var chance))
            return;

        if (!HatchingContext.RollPercentage(chance))
            return;

        if (egg.TryGetValue(RoosterItemDataEntries.Name, out var eggId))
        {
            data[rooster.FarmAnimalName].EggItemIds ??= new();
            data[rooster.FarmAnimalName].EggItemIds.Add(eggId);

            // remove eggs from females/other animals to avoid duplicates
            if (ctx.RoosterHelper.FemaleEggs.TryGetValue(femaleKey, out var femaleEggs)
                && femaleEggs.TryGetValue(eggType, out var femaleEggId))
            {
                data[femaleKey].EggItemIds ??= new();
                data[femaleKey].EggItemIds.Remove(femaleEggId);
            }

            foreach (var otherAnimal in otherAnimalsSharingEggs)
            {
                if (!data.ContainsKey(otherAnimal) || data[otherAnimal].EggItemIds is null)
                    continue;

                data[otherAnimal].EggItemIds.Remove(eggId);
            }
        }
    }
}
