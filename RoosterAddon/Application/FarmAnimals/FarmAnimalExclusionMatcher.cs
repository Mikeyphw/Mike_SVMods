using System;
using System.Collections.Generic;
using RoosterAddon.Framework;
using StardewValley.GameData.FarmAnimals;

namespace RoosterAddon.Application.FarmAnimals;

/// <summary>
/// Unit-testable exclusion matcher.
/// Supports tokens:
/// <list type="bullet">
/// <item><description>direct key: <c>Chicken</c></description></item>
/// <item><description>building: <c>building:Big Coop</c></description></item>
/// <item><description>mod: <c>mod:Author.ModId</c> (or <c>AnimalExclusionsModID</c>)</description></item>
/// <item><description>custom field equality: <c>cf:Field=Value</c></description></item>
/// </list>
/// </summary>
internal sealed class FarmAnimalExclusionMatcher
{
    private static readonly string[] ModIdFieldCandidates =
    {
        "ModID", "ModId", "ModUniqueId", "SourceMod", "OwnerMod"
    };

    private readonly HashSet<string> _excludedKeys;
    private readonly HashSet<string> _excludedBuildings;
    private readonly HashSet<string> _excludedModIds;
    private readonly List<(string Field, string Value)> _customFieldRules;

    private FarmAnimalExclusionMatcher(
        HashSet<string> excludedKeys,
        HashSet<string> excludedBuildings,
        HashSet<string> excludedModIds,
        List<(string Field, string Value)> customFieldRules
    )
    {
        _excludedKeys = excludedKeys;
        _excludedBuildings = excludedBuildings;
        _excludedModIds = excludedModIds;
        _customFieldRules = customFieldRules;
    }

    public static FarmAnimalExclusionMatcher From(AnimalExclusion exclusions)
    {
        exclusions ??= new AnimalExclusion();

        var excludedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var excludedBuildings = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var excludedModIds = new HashSet<string>(
            exclusions.AnimalExclusionsModID ?? new List<string>(),
            StringComparer.OrdinalIgnoreCase
        );
        var customFieldRules = new List<(string Field, string Value)>();

        foreach (var tokenRaw in exclusions.AnimalExclusions ?? new List<string>())
        {
            var token = (tokenRaw ?? string.Empty).Trim();
            if (token.Length == 0)
                continue;

            if (token.StartsWith("building:", StringComparison.OrdinalIgnoreCase))
            {
                var building = token["building:".Length..].Trim();
                if (building.Length > 0)
                    excludedBuildings.Add(building);
                continue;
            }

            if (token.StartsWith("mod:", StringComparison.OrdinalIgnoreCase))
            {
                var modId = token["mod:".Length..].Trim();
                if (modId.Length > 0)
                    excludedModIds.Add(modId);
                continue;
            }

            if (token.StartsWith("cf:", StringComparison.OrdinalIgnoreCase))
            {
                var rule = token["cf:".Length..].Trim();
                var eq = rule.IndexOf('=');
                if (eq > 0 && eq < rule.Length - 1)
                {
                    var field = rule[..eq].Trim();
                    var value = rule[(eq + 1)..].Trim();
                    if (field.Length > 0 && value.Length > 0)
                        customFieldRules.Add((field, value));
                }
                continue;
            }

            excludedKeys.Add(token);
        }

        return new FarmAnimalExclusionMatcher(excludedKeys, excludedBuildings, excludedModIds, customFieldRules);
    }

    public bool IsExcluded(string animalKey, FarmAnimalData? data = null)
    {
        if (string.IsNullOrWhiteSpace(animalKey))
            return false;

        if (_excludedKeys.Contains(animalKey))
            return true;

        if (data is null)
            return false;

        if (!string.IsNullOrWhiteSpace(data.RequiredBuilding) && _excludedBuildings.Contains(data.RequiredBuilding.Trim()))
            return true;

        if (_excludedModIds.Count > 0 && data.CustomFields is not null)
        {
            foreach (var field in ModIdFieldCandidates)
            {
                if (data.CustomFields.TryGetValue(field, out var raw) && !string.IsNullOrWhiteSpace(raw))
                {
                    if (_excludedModIds.Contains(raw.Trim()))
                        return true;
                }
            }
        }

        if (_customFieldRules.Count > 0 && data.CustomFields is not null)
        {
            foreach (var (field, value) in _customFieldRules)
            {
                if (data.CustomFields.TryGetValue(field, out var raw) && !string.IsNullOrWhiteSpace(raw))
                {
                    if (string.Equals(raw.Trim(), value, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
        }

        return false;
    }
}
