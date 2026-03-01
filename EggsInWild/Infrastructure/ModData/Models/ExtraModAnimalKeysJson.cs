using System;
using System.Collections.Generic;
using System.Linq;

namespace EggsInWild.Infrastructure.ModData.Models;

public sealed class ExtraModAnimalKeysJson
{
    /// <summary>
    /// Canonical list of farm animal keys for this mod-group.
    /// This list is treated as unweighted; ordering is preserved.
    /// </summary>
    public List<string> FarmAnimalKeys { get; set; } = new();

    // Legacy input support (pre-Phase 11). Kept to avoid breaking user-edited JSON.
    [Obsolete("Legacy JSON field. Use FarmAnimalKeys instead.")]
    public List<string> FarmAnimalKeysSameWeight { get; set; } = new();

    [Obsolete("Legacy JSON field. Use FarmAnimalKeys instead. Weight is ignored.")]
    public List<LegacyIdJson> FarmAnimalKeysWeighted { get; set; } = new();

    /// <summary>Enumerate canonical keys, falling back to legacy fields when needed.</summary>
    public IReadOnlyList<string> GetEffectiveFarmAnimalKeys()
    {
        var raw = FarmAnimalKeys is { Count: > 0 }
            ? FarmAnimalKeys
            : (FarmAnimalKeysSameWeight.Count > 0 || FarmAnimalKeysWeighted.Count > 0)
                ? FarmAnimalKeysSameWeight.Concat(FarmAnimalKeysWeighted.Select(x => x.Id)).ToList()
                : new List<string>();

        // De-dupe while preserving order.
        var outList = new List<string>(raw.Count);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var k in raw)
        {
            var t = (k ?? string.Empty).Trim();
            if (t.Length == 0)
                continue;

            if (seen.Add(t))
                outList.Add(t);
        }

        return outList;
    }
}

public sealed class LegacyIdJson
{
    public string Id { get; set; } = "";
}
