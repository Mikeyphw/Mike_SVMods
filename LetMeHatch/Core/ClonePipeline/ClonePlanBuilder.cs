using System;
using System.Collections.Generic;
using System.Linq;
using LetMeHatch.Core.Cloning;
using static LetMeHatch.EnumsAndStrings.Enums;

namespace LetMeHatch.Core.ClonePipeline;

/// <summary>
/// Default clone plan builder using animal produce snapshots.
/// This is intentionally conservative and does not depend on Stardew types.
/// </summary>
public sealed class ClonePlanBuilder : IClonePlanBuilder
{
    public ClonePlan Build(ClonePlanBuildRequest request)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));
        if (string.IsNullOrWhiteSpace(request.ModUniqueId))
            throw new ArgumentException("Mod unique id must be non-empty.", nameof(request));

        var items = new List<CloneSpec>();

        foreach (var (animalKey, produce) in request.Animals.OrderBy(static kvp => kvp.Key, StringComparer.OrdinalIgnoreCase))
        {
            if (produce is null)
                continue;

            foreach (var originalId in (produce.ProduceItemIds ?? Array.Empty<string>())
                         .Where(static s => !string.IsNullOrWhiteSpace(s))
                         .OrderBy(static s => s, StringComparer.Ordinal))
            {
                items.Add(BuildSpec(request.ModUniqueId, animalKey, originalId, ItemType.Normal));
            }

            foreach (var originalId in (produce.DeluxeProduceItemIds ?? Array.Empty<string>())
                         .Where(static s => !string.IsNullOrWhiteSpace(s))
                         .OrderBy(static s => s, StringComparer.Ordinal))
            {
                items.Add(BuildSpec(request.ModUniqueId, animalKey, originalId, ItemType.Large));
            }
        }

        return new ClonePlan(items);
    }

    private static CloneSpec BuildSpec(string modUniqueId, string animalKey, string originalId, ItemType itemType)
    {
        var cloneType = CloneItemType.Egg;
        var cloneId = CloneIdService.GetClonedItemId(modUniqueId, animalKey, originalId, cloneType, itemType, isFertilized: false);
        var seedKey = $"{animalKey}:{originalId}:{cloneType}:{itemType}";
        return new CloneSpec(animalKey, originalId, cloneId, cloneType, itemType, false, seedKey);
    }
}
