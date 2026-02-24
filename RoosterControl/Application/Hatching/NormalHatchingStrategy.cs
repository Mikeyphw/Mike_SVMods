using System.Collections.Generic;
using RoosterControl.Application.Hatching.Decisions;
using RoosterControl.Constants;
using RoosterControl.Enums;
using StardewValley.GameData.FarmAnimals;

namespace RoosterControl.Application.Hatching;

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

        if (!rooster.Eggs.TryGetValue(eggType, out var egg))
            return;

        if (!egg.TryGetValue(RoosterItemDataEntries.Probability, out var rawChance))
            return;

        if (!int.TryParse(rawChance, out var chance))
            return;

        if (!egg.TryGetValue(RoosterItemDataEntries.Name, out var eggId) || string.IsNullOrWhiteSpace(eggId))
            return;

        string? femaleEggId = null;
        ctx.RoosterHelper.FemaleEggs.TryGetValue(femaleKey, out var femaleEggs);
        femaleEggs?.TryGetValue(eggType, out femaleEggId);

        var decision = HatchingDecisionEngine.DecideEggChance(
            new EggChanceRule(
                Enabled: true,
                ChancePercent: chance,
                EggId: eggId,
                FemaleEggIdToRemove: femaleEggId
            ),
            ctx.Roller
        );

        if (decision is null)
            return;

        data[rooster.FarmAnimalName].EggItemIds ??= new();
        data[rooster.FarmAnimalName].EggItemIds.Add(decision.EggIdToAddToRooster);

        if (!string.IsNullOrWhiteSpace(decision.FemaleEggIdToRemove))
        {
            data[femaleKey].EggItemIds ??= new();
            data[femaleKey].EggItemIds.Remove(decision.FemaleEggIdToRemove);
        }

        foreach (var otherAnimal in otherAnimalsSharingEggs)
        {
            if (!data.ContainsKey(otherAnimal) || data[otherAnimal].EggItemIds is null)
                continue;

            data[otherAnimal].EggItemIds.Remove(decision.EggIdToAddToRooster);
        }
    }
}
