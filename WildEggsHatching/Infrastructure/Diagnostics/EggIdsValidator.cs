using System;
using System.Collections.Generic;
using System.Linq;
using StardewValley.GameData.Objects;

namespace WildEggsHatching.Infrastructure.Diagnostics;

internal static class EggIdsValidator
{
    public static (WeggVerifyStatus Status, string Message) Validate(
        IReadOnlyList<string> eggItemIds,
        IReadOnlyDictionary<string, ObjectData>? objects,
        bool full)
    {
        if (eggItemIds is null || eggItemIds.Count == 0)
            return (WeggVerifyStatus.Warn, "No egg item IDs were produced from SpawnAreas (check ModData/SpawnAreas.json).");

        if (objects is null || objects.Count == 0)
            return (WeggVerifyStatus.Warn, "Could not load Data/Objects to validate egg IDs.");

        var missing = eggItemIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(id => !objects.ContainsKey(id))
            .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (missing.Count == 0)
            return (WeggVerifyStatus.Pass, $"Egg IDs OK (eggs={eggItemIds.Count}).");

        var sampleN = full ? missing.Count : Math.Min(10, missing.Count);
        var sample = missing.Take(sampleN).ToList();
        var suffix = missing.Count > sampleN ? $" (+{missing.Count - sampleN} more)" : string.Empty;

        return (WeggVerifyStatus.Fail,
            $"Egg IDs missing from Data/Objects: missing={missing.Count}. MissingEggItemIds=[{string.Join(", ", sample)}]{suffix}");
    }
}
