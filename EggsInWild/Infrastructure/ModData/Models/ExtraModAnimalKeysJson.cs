using System;
using System.Collections.Generic;

namespace EggsInWild.Infrastructure.ModData.Models;

public sealed class ExtraModAnimalKeysJson
{
    /// <summary>
    /// Canonical list of farm animal keys for this mod-group.
    /// This list is treated as unweighted; ordering is preserved.
    /// </summary>
    public List<string> FarmAnimalKeys { get; set; } = new();

    /// <summary>
    /// Enumerate canonical keys with normalization (trim + de-dupe, order preserved).
    /// </summary>
    public IReadOnlyList<string> GetEffectiveFarmAnimalKeys()
    {
        if (FarmAnimalKeys.Count == 0)
            return Array.Empty<string>();

        var outList = new List<string>(FarmAnimalKeys.Count);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var k in FarmAnimalKeys)
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
