using System;
using System.Collections.Generic;
using System.Linq;
using RoosterAddon.Framework;
using StardewModdingAPI;

namespace RoosterAddon.Application.FarmAnimals;

/// <summary>
/// Loads and normalizes user exclusions from <c>data/exclusions.json</c>.
/// </summary>
internal sealed class FarmAnimalExclusionsService
{
    public AnimalExclusion LoadOrCreate(IModHelper helper)
    {
        if (helper is null) throw new ArgumentNullException(nameof(helper));

        var raw = helper.Data.ReadJsonFile<AnimalExclusion>("exclusions.json") ?? new AnimalExclusion();
        helper.Data.WriteJsonFile("exclusions.json", raw);

        return new AnimalExclusion
        {
            AnimalExclusions = NormalizeTokens(raw.AnimalExclusions),
            AnimalExclusionsModID = NormalizeModIds(raw.AnimalExclusionsModID)
        };
    }

    internal static List<string> NormalizeTokens(IEnumerable<string> rawEntries)
    {
        var tokens = new List<string>();

        foreach (var entry in rawEntries ?? Array.Empty<string>())
        {
            if (string.IsNullOrWhiteSpace(entry))
                continue;

            foreach (var part in entry.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                var token = part.Trim();
                if (token.Length == 0)
                    continue;

                if (token.Equals("OR", StringComparison.OrdinalIgnoreCase))
                    continue;

                tokens.Add(token);
            }
        }

        return tokens
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    internal static List<string> NormalizeModIds(IEnumerable<string> rawIds)
    {
        return (rawIds ?? Array.Empty<string>())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
