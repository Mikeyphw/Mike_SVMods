using System;
using System.Collections.Generic;
using System.Linq;
using EggsInWild.Infrastructure.Mods;
using EggsInWild.Infrastructure.ModData.Models;

namespace EggsInWild.Infrastructure.Diagnostics;

public static class ExtraModAnimalKeysWarnings
{
    public static List<string> Build(
        Dictionary<string, ExtraModAnimalKeysJson> modGroups,
        IModAvailability mods,
        ISet<string>? knownFarmAnimalIds)
    {
        var warnings = new List<string>();

        foreach (var (modId, block) in modGroups.OrderBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(modId))
            {
                warnings.Add("ExtraModAnimalKeys contains a blank modId key; block will be ignored.");
                continue;
            }

            var isVanilla = string.Equals(modId, "Vanilla", StringComparison.OrdinalIgnoreCase);
            if (!isVanilla && !mods.IsLoaded(modId))
                warnings.Add($"ExtraModAnimalKeys modId '{modId}' is not loaded; its keys will be ignored.");

            var raw = block.FarmAnimalKeys ?? new List<string>();
            if (raw.Count == 0)
            {
                warnings.Add($"ExtraModAnimalKeys '{modId}' has no FarmAnimalKeys.");
                continue;
            }

            var blanks = raw.Count(s => string.IsNullOrWhiteSpace(s));
            if (blanks > 0)
                warnings.Add($"ExtraModAnimalKeys '{modId}' has {blanks} blank FarmAnimalKeys entries.");

            var whitespace = raw.Count(s => !string.IsNullOrWhiteSpace(s) && s != s.Trim());
            if (whitespace > 0)
                warnings.Add($"ExtraModAnimalKeys '{modId}' has {whitespace} keys with leading/trailing whitespace.");

            var trimmed = raw
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .Where(s => s.Length > 0)
                .ToList();

            var dupes = trimmed
                .GroupBy(s => s, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                .Take(10)
                .ToList();

            if (dupes.Count > 0)
                warnings.Add($"ExtraModAnimalKeys '{modId}' has duplicate keys (case-insensitive): {string.Join(", ", dupes)}.");

            var effective = block.GetEffectiveFarmAnimalKeys();
            if (knownFarmAnimalIds is not null)
            {
                var unknown = effective
                    .Where(x => !knownFarmAnimalIds.Contains(x))
                    .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                    .Take(10)
                    .ToList();

                if (unknown.Count > 0)
                    warnings.Add($"ExtraModAnimalKeys '{modId}' has FarmAnimalKeys not present in Data/FarmAnimals: {string.Join(", ", unknown)}.");
            }

            if (effective.Count == 0)
                warnings.Add($"ExtraModAnimalKeys '{modId}' has no effective FarmAnimalKeys after normalization.");
        }

        return warnings;
    }
}
