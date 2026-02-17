using System;
using System.Collections.Generic;
using System.Linq;
using RoosterAddon.Constants;
using RoosterAddon.Enums;
using StardewValley.GameData.FarmAnimals;

namespace RoosterAddon.Application.Hatching;

/// <summary>
/// Applies hatching-related edits to <c>Data/FarmAnimals</c>.
/// </summary>
internal sealed class FarmAnimalHatchingService
{
    private readonly IHatchingStrategy _normal = new NormalHatchingStrategy();
    private readonly IHatchingStrategy _fertilized = new FertilizedOnlyHatchingStrategy();

    public void ApplyForRooster(
        HatchingContext ctx,
        RoosterData rooster,
        IDictionary<string, FarmAnimalData> data
    )
    {
        if (rooster.Females.Count == 0)
            return;

        var otherAnimals = GetAnimalsThatProduceRoosterEgg(data, rooster);
        var strategy = ctx.IsFertilizedOnly ? _fertilized : _normal;

        foreach (var female in rooster.Females)
            strategy.Apply(ctx, female, rooster, data, otherAnimals);
    }

    private static List<string> GetAnimalsThatProduceRoosterEgg(
        IDictionary<string, FarmAnimalData> animalData,
        RoosterData rooster
    )
    {
        var roosterEggIds = rooster
            .Eggs.SelectMany(e => e.Value)
            .Where(e => e.Key == RoosterItemDataEntries.Name)
            .Select(e => e.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return animalData
            .Where(a => a.Value.EggItemIds is not null && a.Value.EggItemIds.Any(e => roosterEggIds.Contains(e)))
            .Select(a => a.Key)
            .ToList();
    }
}
