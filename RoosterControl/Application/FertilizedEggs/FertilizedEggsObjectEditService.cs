using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using RoosterControl.Enums;
using StardewValley.GameData.Objects;

namespace RoosterControl.Application.FertilizedEggs;

/// <summary>
/// Creates fertilized egg object variants and ensures <see cref="RoosterControl.Helpers.RoosterHelper.FemaleEggs"/>
/// has a fertilized mapping for each known female.
/// </summary>
internal sealed class FertilizedEggsObjectEditService
{
    private static readonly Regex TrailingNumberRegex = new(@"^(.*?)(\d+)$", RegexOptions.Compiled);

    public static string GetSynthesizedFertilizedId(string baseEggId)
    {
        if (string.IsNullOrWhiteSpace(baseEggId))
            throw new ArgumentException("Base egg ID is required.", nameof(baseEggId));

        return baseEggId.EndsWith("_Fertilized", StringComparison.OrdinalIgnoreCase)
            ? baseEggId
            : $"{baseEggId}_Fertilized";
    }

    public void Apply(
        Dictionary<string, ObjectData> objects,
        IDictionary<string, Dictionary<EggTypeEntries, string>> femaleEggs,
        Func<string> getFertilizedNameSuffix,
        Func<string> getFertilizedDescription,
        bool enableSynthesis
    )
    {
        if (objects is null)
            throw new ArgumentNullException(nameof(objects));
        if (femaleEggs is null)
            throw new ArgumentNullException(nameof(femaleEggs));
        if (getFertilizedNameSuffix is null)
            throw new ArgumentNullException(nameof(getFertilizedNameSuffix));
        if (getFertilizedDescription is null)
            throw new ArgumentNullException(nameof(getFertilizedDescription));

        if (!enableSynthesis)
            return;

        foreach (var (femaleKey, eggsByType) in femaleEggs)
        {
            if (eggsByType is null)
                continue;

            if (eggsByType.TryGetValue(EggTypeEntries.Fertilized, out var existingFert)
                && !string.IsNullOrWhiteSpace(existingFert)
                && objects.ContainsKey(existingFert))
            {
                continue;
            }

            var baseEggId = ResolveBaseEggId(eggsByType, objects);
            if (string.IsNullOrWhiteSpace(baseEggId))
                continue;

            var fertId = GetSynthesizedFertilizedId(baseEggId);

            if (!objects.ContainsKey(fertId) && objects.TryGetValue(baseEggId, out var baseEgg))
            {
                objects[fertId] = CreateFertilizedEgg(
                    baseEgg,
                    nameSuffix: getFertilizedNameSuffix(),
                    description: getFertilizedDescription()
                );
            }

            eggsByType[EggTypeEntries.Fertilized] = fertId;
        }
    }

    private static string? ResolveBaseEggId(
        IDictionary<EggTypeEntries, string> eggsByType,
        Dictionary<string, ObjectData> objects
    )
    {
        if (eggsByType.TryGetValue(EggTypeEntries.Large, out var large) && !string.IsNullOrWhiteSpace(large))
            return ResolveLargeEggIfNeeded(large, objects);

        if (eggsByType.TryGetValue(EggTypeEntries.Normal, out var normal) && !string.IsNullOrWhiteSpace(normal))
            return ResolveLargeEggIfNeeded(normal, objects);

        return null;
    }

    private static string ResolveLargeEggIfNeeded(string eggId, Dictionary<string, ObjectData> objects)
    {
        var match = TrailingNumberRegex.Match(eggId);
        if (!match.Success)
            return eggId;

        var prefix = match.Groups[1].Value;
        var id = match.Groups[2].Value;

        if (id is not ("176" or "180"))
            return eggId;

        var largeId = id == "176" ? "174" : "182";
        var largeEggId = prefix + largeId;
        return objects.ContainsKey(largeEggId) ? largeEggId : eggId;
    }

    private static ObjectData CreateFertilizedEgg(ObjectData baseEgg, string nameSuffix, string description)
    {
        if (baseEgg is null)
            throw new ArgumentNullException(nameof(baseEgg));

        var clone = new ObjectData();
        var type = typeof(ObjectData);

        foreach (var prop in type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
        {
            if (!prop.CanRead || !prop.CanWrite)
                continue;

            try
            {
                prop.SetValue(clone, prop.GetValue(baseEgg));
            }
            catch
            {
                // Some properties may throw due to runtime-only backing data; ignore.
            }
        }

        if (!string.IsNullOrWhiteSpace(clone.DisplayName))
            clone.DisplayName = $"{clone.DisplayName} ({nameSuffix})";
        else if (!string.IsNullOrWhiteSpace(baseEgg.DisplayName))
            clone.DisplayName = $"{baseEgg.DisplayName} ({nameSuffix})";

        if (!string.IsNullOrWhiteSpace(description))
            clone.Description = description;

        clone.ContextTags ??= new();
        if (!clone.ContextTags.Contains("roostercontrol_fertilized_egg"))
            clone.ContextTags.Add("roostercontrol_fertilized_egg");

        return clone;
    }
}
