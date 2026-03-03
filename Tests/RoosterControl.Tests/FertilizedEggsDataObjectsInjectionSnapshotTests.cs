using System;
using System.Collections.Generic;
using System.Linq;
using RoosterControl.Application.FertilizedEggs;
using RoosterControl.Enums;
using StardewValley.GameData.Objects;
using Xunit;

namespace RoosterControl.Tests;

public sealed class FertilizedEggsDataObjectsInjectionSnapshotTests
{
    [Fact]
    public void Apply_GoldenSnapshot_CreatesExpectedKeys_AndMappingsOnly()
    {
        var objects = new Dictionary<string, ObjectData>(StringComparer.OrdinalIgnoreCase)
        {
            ["ModEgg_176"] = new ObjectData { DisplayName = "Small Egg" },
            ["ModEgg_174"] = new ObjectData { DisplayName = "Large Egg" },
            ["DuckEgg_180"] = new ObjectData { DisplayName = "Duck Egg" },

            ["Existing_Fert"] = new ObjectData { DisplayName = "Existing Fertilized Egg" }
        };

        var femaleEggs = new Dictionary<string, Dictionary<EggTypeEntries, string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Hen"] = new()
            {
                [EggTypeEntries.Normal] = "ModEgg_176"
            },
            ["Duck"] = new()
            {
                [EggTypeEntries.Normal] = "DuckEgg_180"
            },
            ["PreWired"] = new()
            {
                [EggTypeEntries.Normal] = "ModEgg_176",
                [EggTypeEntries.Fertilized] = "Existing_Fert"
            },
            ["MissingEggFemale"] = new()
            {
                [EggTypeEntries.Normal] = "NoSuchEgg"
            }
        };

        var expectedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ModEgg_176",
            "ModEgg_174",
            "DuckEgg_180",
            "Existing_Fert",
            "ModEgg_174_Fertilized",
            "DuckEgg_180_Fertilized"
        };

        var svc = new FertilizedEggsObjectEditService();

        svc.Apply(
            objects: objects,
            femaleEggs: femaleEggs,
            getFertilizedNameSuffix: () => "Fertilized",
            getFertilizedDescription: () => "Fertilized description",
            enableSynthesis: true
        );

        Assert.True(expectedKeys.SetEquals(objects.Keys), "Object keys changed unexpectedly.");
        Assert.Equal("ModEgg_174_Fertilized", femaleEggs["Hen"][EggTypeEntries.Fertilized]);
        Assert.Equal("DuckEgg_180_Fertilized", femaleEggs["Duck"][EggTypeEntries.Fertilized]);
        Assert.Equal("Existing_Fert", femaleEggs["PreWired"][EggTypeEntries.Fertilized]);
        Assert.False(femaleEggs["MissingEggFemale"].ContainsKey(EggTypeEntries.Fertilized));

        Assert.Contains("roostercontrol_fertilized_egg", objects["ModEgg_174_Fertilized"].ContextTags);
        Assert.Contains("roostercontrol_fertilized_egg", objects["DuckEgg_180_Fertilized"].ContextTags);
    }
}
