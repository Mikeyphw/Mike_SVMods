using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using RoosterAddon.Constants;
using RoosterAddon.Enums;
using StardewValley.GameData.Objects;

namespace RoosterAddon.Application;

internal sealed partial class RoosterCoordinator
{
private void GetAndSetFertilizedEggs(
            Dictionary<string, ObjectData> data,
            Dictionary<string, ObjectData> originalEggs,
            bool mearasegg = false
        )
        {
            var RoosterDefaultFemales = RoosterHelper.Roosters.Select(r => r.Value.Female).ToList();

            if (GetHatchApiCloneEggsOption())
            {
                foreach (
                    var femaleEgg in RoosterHelper.FemaleEggs.Where(e =>
                        !RoosterDefaultFemales.Contains(e.Key)
                    )
                )
                {
                    var key = femaleEgg.Key;
                    var eggEntry = femaleEgg
                        .Value.Where(e => e.Key != EggTypeEntries.Fertilized)
                        .ToDictionary(x => x.Key, x => x.Value);
                    var fertilizedEggs = new Dictionary<string, string>();

                    foreach (
                        var (eggType, _eggFullID) in eggEntry.Where(e =>
                            data[e.Value].CustomFields != null
                            && data[e.Value].CustomFields.ContainsKey("originalEgg")
                        )
                    )
                    {
                        var smallEggs = new List<string> { "176", "180" };
                        var eggFullID = "";
                        string patternEgg = @"(^.*_)(\d+)$";
                        if (
                            smallEggs.Contains(Regex.Replace(_eggFullID, patternEgg, "$2"))
                            && CheckIfLargeEggExists(_eggFullID, data)
                        )
                        {
                            eggFullID = data[_eggFullID].CustomFields["originalEgg"] switch
                            {
                                "176" => Regex.Replace(_eggFullID, patternEgg, "$1") + "174",
                                _ => Regex.Replace(_eggFullID, patternEgg, "$1") + "182"
                            };
                        }
                        else
                        {
                            eggFullID = _eggFullID;
                        }
                        var egg = originalEggs[eggFullID];
                        if (mearasegg)
                        {
                            egg = originalEggs[GetFertilizedEggId(eggFullID)];
                        }
                        var newEgg = CreateFertilizedEgg(egg);
                        data[$"{eggFullID}_Fertilized"] = newEgg;
                        fertilizedEggs[eggFullID] = $"{eggFullID}_Fertilized";
                    }
                    foreach (var (keyEgg, value) in fertilizedEggs)
                    {
                        RoosterHelper.FemaleEggs[key][EggTypeEntries.Fertilized] = value;
                    }
                }
            }
            foreach (var defaultFemale in RoosterDefaultFemales)
            {
                var rooster = RoosterHelper
                    .Roosters.Select(r => r.Value)
                    .First(r => r.Female == defaultFemale);
                RoosterHelper.FemaleEggs[defaultFemale][EggTypeEntries.Fertilized] = rooster.Eggs[
                    EggTypeEntries.Fertilized
                ][RoosterItemDataEntries.Name];
            }
        }

internal bool CheckIfLargeEggExists(string normalEgg, Dictionary<string, ObjectData> data)
        {
            string patternEgg = @"(^.*_)(\d+)$";
            var largeEgg = Regex.Replace(normalEgg, patternEgg, "$2") switch
            {
                "176" => "174",
                _ => "182"
            };
            var largeEggID = Regex.Replace(normalEgg, patternEgg, "$1") + largeEgg;
            return data.ContainsKey(largeEggID);
        }

private string GetFertilizedEggId(string eggFullID)
        {
            string pattern = @"^.*_(\d+)$";
            string eggID = Regex.Replace(eggFullID, pattern, "$1");
            return eggID switch
            {
                ItemIds.AnimalProducts.BrownEgg
                or ItemIds.AnimalProducts.LargeBrownEgg
                    => ItemIds.CustomItems.FertilizedBrownEgg,
                ItemIds.AnimalProducts.WhiteEgg
                or ItemIds.AnimalProducts.LargeWhiteEgg
                    => ItemIds.CustomItems.FertilizedWhiteEgg,
                ItemIds.AnimalProducts.VoidEgg
                or ItemIds.AnimalProducts.LargeBrownEgg
                    => ItemIds.CustomItems.FertilizedVoidEgg,
                ItemIds.AnimalProducts.GoldenEgg => ItemIds.CustomItems.FertilizedGoldEgg,
                _ => ItemIds.AnimalProducts.WhiteEgg
            };
        }

        private ObjectData CreateFertilizedEgg(ObjectData baseEgg)
        {
            if (baseEgg is null)
                throw new ArgumentNullException(nameof(baseEgg));

            // Clone via reflection to preserve game data fields without maintaining a manual copy list.
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
                    // Some properties may throw due to runtime-only backing data; ignore and keep defaults.
                }
            }

            clone.CustomFields ??= new Dictionary<string, string>();

            if (!clone.CustomFields.ContainsKey("originalEgg") && baseEgg.CustomFields is not null && baseEgg.CustomFields.TryGetValue("originalEgg", out var originalEgg))
                clone.CustomFields["originalEgg"] = originalEgg;

            var suffix = Helper.Translation.Get("egg.fertilized.name");
            var desc = Helper.Translation.Get("egg.fertilized.description");

            if (!string.IsNullOrWhiteSpace(clone.DisplayName))
                clone.DisplayName = $"{clone.DisplayName} ({suffix})";
            else if (!string.IsNullOrWhiteSpace(baseEgg.DisplayName))
                clone.DisplayName = $"{baseEgg.DisplayName} ({suffix})";

            if (!string.IsNullOrWhiteSpace(desc))
                clone.Description = desc;

            clone.ContextTags ??= new();
            if (!clone.ContextTags.Contains("roosteraddon_fertilized_egg"))
                clone.ContextTags.Add("roosteraddon_fertilized_egg");

            return clone;
        }

}
