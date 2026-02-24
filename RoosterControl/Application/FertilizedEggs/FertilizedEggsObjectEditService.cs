using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using RoosterControl.Constants;
using RoosterControl.Enums;
using StardewValley.GameData.Objects;

namespace RoosterControl.Application.FertilizedEggs;

/// <summary>
/// Creates and assigns fertilized egg object variants based on existing egg entries.
/// </summary>
internal sealed class FertilizedEggsObjectEditService
{
    private static readonly Regex EggSuffixRegex = new(@"(^.*_)(\d+)$", RegexOptions.Compiled);

    public void Apply(
        Dictionary<string, ObjectData> objects,
        Dictionary<string, ObjectData> originalEggs,
        IDictionary<string, Dictionary<EggTypeEntries, string>> femaleEggs,
        IEnumerable<string> defaultFemales,
        Func<string> getFertilizedNameSuffix,
        Func<string> getFertilizedDescription,
        bool cloneEggsEnabled,
        bool includeMearasEggs = false
    )
    {
        if (objects is null)
            throw new ArgumentNullException(nameof(objects));
        if (originalEggs is null)
            throw new ArgumentNullException(nameof(originalEggs));
        if (femaleEggs is null)
            throw new ArgumentNullException(nameof(femaleEggs));
        if (defaultFemales is null)
            throw new ArgumentNullException(nameof(defaultFemales));

        var defaultFemaleSet = defaultFemales.ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (cloneEggsEnabled)
        {
            foreach (var (female, eggsByType) in femaleEggs.Where(p => !defaultFemaleSet.Contains(p.Key)))
            {
                var eggEntries = eggsByType
                    .Where(e => e.Key != EggTypeEntries.Fertilized)
                    .ToDictionary(x => x.Key, x => x.Value);

                var fertilizedMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                foreach (var (_, eggFullId) in eggEntries)
                {
                    if (!objects.TryGetValue(eggFullId, out var eggObj))
                        continue;

                    if (eggObj.CustomFields is null || !eggObj.CustomFields.ContainsKey("originalEgg"))
                        continue;

                    var resolvedEggFullId = ResolveLargeEggIfNeeded(eggFullId, objects);
                    if (includeMearasEggs)
                        resolvedEggFullId = GetFertilizedEggId(resolvedEggFullId);

                    if (!originalEggs.TryGetValue(resolvedEggFullId, out var baseEgg))
                        continue;

                    var newId = $"{resolvedEggFullId}_Fertilized";
                    objects[newId] = CreateFertilizedEgg(
                        baseEgg,
                        nameSuffix: getFertilizedNameSuffix(),
                        description: getFertilizedDescription()
                    );

                    fertilizedMap[resolvedEggFullId] = newId;
                }

                foreach (var (baseEggId, fertId) in fertilizedMap)
                    femaleEggs[female][EggTypeEntries.Fertilized] = fertId;
            }
        }
    }

    private static string ResolveLargeEggIfNeeded(string eggFullId, Dictionary<string, ObjectData> objects)
    {
        var match = EggSuffixRegex.Match(eggFullId);
        if (!match.Success)
            return eggFullId;

        var id = match.Groups[2].Value;
        var isSmall = id is "176" or "180";
        if (!isSmall)
            return eggFullId;

        var largeId = id == "176" ? "174" : "182";
        var largeEggFullId = match.Groups[1].Value + largeId;
        return objects.ContainsKey(largeEggFullId) ? largeEggFullId : eggFullId;
    }

    private static string GetFertilizedEggId(string eggFullId)
    {
        var eggId = EggSuffixRegex.Replace(eggFullId, "$2");
        return eggId switch
        {
            ItemIds.AnimalProducts.BrownEgg
            or ItemIds.AnimalProducts.LargeBrownEgg => ItemIds.CustomItems.FertilizedBrownEgg,

            ItemIds.AnimalProducts.WhiteEgg
            or ItemIds.AnimalProducts.LargeWhiteEgg => ItemIds.CustomItems.FertilizedWhiteEgg,

            ItemIds.AnimalProducts.VoidEgg => ItemIds.CustomItems.FertilizedVoidEgg,

            ItemIds.AnimalProducts.GoldenEgg => ItemIds.CustomItems.FertilizedGoldEgg,

            _ => ItemIds.CustomItems.FertilizedWhiteEgg
        };
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

        clone.CustomFields ??= new Dictionary<string, string>();

        if (!clone.CustomFields.ContainsKey("originalEgg")
            && baseEgg.CustomFields is not null
            && baseEgg.CustomFields.TryGetValue("originalEgg", out var originalEgg))
        {
            clone.CustomFields["originalEgg"] = originalEgg;
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
