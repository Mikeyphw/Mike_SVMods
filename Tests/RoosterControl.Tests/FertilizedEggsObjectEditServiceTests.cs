using System;
using System.Collections.Generic;
using RoosterControl.Application.FertilizedEggs;
using RoosterControl.Enums;
using StardewValley.GameData.Objects;
using Xunit;

namespace RoosterControl.Tests;

public sealed class FertilizedEggsObjectEditServiceTests
{
    [Fact]
    public void Apply_CreatesFertilizedVariant_WithExactId_AndAssignsFemaleMapping()
    {
        var objects = new Dictionary<string, ObjectData>(StringComparer.OrdinalIgnoreCase)
        {
            ["ModEgg_176"] = new ObjectData
            {
                DisplayName = "Small Egg",
                Description = "small",
                CustomFields = new Dictionary<string, string> { ["originalEgg"] = "176" }
            },
            ["ModEgg_174"] = new ObjectData
            {
                DisplayName = "Large Egg",
                Description = "large",
                CustomFields = new Dictionary<string, string> { ["originalEgg"] = "174" }
            }
        };

        var femaleEggs = new Dictionary<string, Dictionary<EggTypeEntries, string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["CustomHen"] = new()
            {
                [EggTypeEntries.Normal] = "ModEgg_176"
            }
        };

        var svc = new FertilizedEggsObjectEditService();

        svc.Apply(
            objects: objects,
            femaleEggs: femaleEggs,
            getFertilizedNameSuffix: () => "Fertilized",
            getFertilizedDescription: () => "Fertilized description",
            enableSynthesis: true
        );

        var expectedId = "ModEgg_174_Fertilized";
        Assert.True(objects.ContainsKey(expectedId));
        Assert.Equal(expectedId, femaleEggs["CustomHen"][EggTypeEntries.Fertilized]);

        var created = objects[expectedId];
        Assert.Contains("roostercontrol_fertilized_egg", created.ContextTags);
        Assert.Equal("174", created.CustomFields["originalEgg"]);
    }

    [Fact]
    public void Apply_UsesSmallEgg_WhenNoLargeVariantExists()
    {
        var objects = new Dictionary<string, ObjectData>(StringComparer.OrdinalIgnoreCase)
        {
            ["ModEgg_176"] = new ObjectData
            {
                DisplayName = "Small Egg",
                Description = "small",
                CustomFields = new Dictionary<string, string> { ["originalEgg"] = "176" }
            }
        };

        var femaleEggs = new Dictionary<string, Dictionary<EggTypeEntries, string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["CustomHen"] = new()
            {
                [EggTypeEntries.Normal] = "ModEgg_176"
            }
        };

        var svc = new FertilizedEggsObjectEditService();

        svc.Apply(
            objects: objects,
            femaleEggs: femaleEggs,
            getFertilizedNameSuffix: () => "Fertilized",
            getFertilizedDescription: () => "Fertilized description",
            enableSynthesis: true
        );

        var expectedId = "ModEgg_176_Fertilized";
        Assert.True(objects.ContainsKey(expectedId));
        Assert.Equal(expectedId, femaleEggs["CustomHen"][EggTypeEntries.Fertilized]);
    }

    [Fact]
    public void Apply_DoesNotOverwriteExistingFertilizedId_WhenItExistsInObjects()
    {
        var objects = new Dictionary<string, ObjectData>(StringComparer.OrdinalIgnoreCase)
        {
            ["ModEgg_176"] = new ObjectData { DisplayName = "Small Egg" },
            ["Existing_Fert"] = new ObjectData { DisplayName = "Existing Fertilized Egg" }
        };

        var femaleEggs = new Dictionary<string, Dictionary<EggTypeEntries, string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["CustomHen"] = new()
            {
                [EggTypeEntries.Normal] = "ModEgg_176",
                [EggTypeEntries.Fertilized] = "Existing_Fert"
            }
        };

        var svc = new FertilizedEggsObjectEditService();

        svc.Apply(
            objects: objects,
            femaleEggs: femaleEggs,
            getFertilizedNameSuffix: () => "Fertilized",
            getFertilizedDescription: () => "Fertilized description",
            enableSynthesis: true
        );

        Assert.Equal("Existing_Fert", femaleEggs["CustomHen"][EggTypeEntries.Fertilized]);
        Assert.False(objects.ContainsKey("ModEgg_176_Fertilized"));
    }

    [Fact]
    public void Apply_DoesNothing_WhenSynthesisDisabled()
    {
        var objects = new Dictionary<string, ObjectData>(StringComparer.OrdinalIgnoreCase)
        {
            ["ModEgg_176"] = new ObjectData { DisplayName = "Small Egg" }
        };

        var femaleEggs = new Dictionary<string, Dictionary<EggTypeEntries, string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["CustomHen"] = new()
            {
                [EggTypeEntries.Normal] = "ModEgg_176"
            }
        };

        var svc = new FertilizedEggsObjectEditService();

        svc.Apply(
            objects: objects,
            femaleEggs: femaleEggs,
            getFertilizedNameSuffix: () => "Fertilized",
            getFertilizedDescription: () => "Fertilized description",
            enableSynthesis: false
        );

        Assert.False(femaleEggs["CustomHen"].ContainsKey(EggTypeEntries.Fertilized));
        Assert.False(objects.ContainsKey("ModEgg_176_Fertilized"));
    }
}
