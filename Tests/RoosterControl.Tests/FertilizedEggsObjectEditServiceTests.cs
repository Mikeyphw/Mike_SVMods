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

        var originalEggs = new Dictionary<string, ObjectData>(StringComparer.OrdinalIgnoreCase)
        {
            ["ModEgg_176"] = objects["ModEgg_176"],
            ["ModEgg_174"] = objects["ModEgg_174"]
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
            originalEggs: originalEggs,
            femaleEggs: femaleEggs,
            defaultFemales: Array.Empty<string>(),
            getFertilizedNameSuffix: () => "Fertilized",
            getFertilizedDescription: () => "Fertilized description",
            cloneEggsEnabled: true,
            includeMearasEggs: false
        );

        var expectedId = "ModEgg_174_Fertilized";
        Assert.True(objects.ContainsKey(expectedId));
        Assert.Equal(expectedId, femaleEggs["CustomHen"][EggTypeEntries.Fertilized]);

        var created = objects[expectedId];
        Assert.Equal("Large Egg (Fertilized)", created.DisplayName);
        Assert.Equal("Fertilized description", created.Description);
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

        var originalEggs = new Dictionary<string, ObjectData>(StringComparer.OrdinalIgnoreCase)
        {
            ["ModEgg_176"] = objects["ModEgg_176"]
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
            originalEggs: originalEggs,
            femaleEggs: femaleEggs,
            defaultFemales: Array.Empty<string>(),
            getFertilizedNameSuffix: () => "Fertilized",
            getFertilizedDescription: () => "Fertilized description",
            cloneEggsEnabled: true,
            includeMearasEggs: false
        );

        var expectedId = "ModEgg_176_Fertilized";
        Assert.True(objects.ContainsKey(expectedId));
        Assert.Equal(expectedId, femaleEggs["CustomHen"][EggTypeEntries.Fertilized]);
    }

    [Fact]
    public void Apply_SkipsDefaultFemales()
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

        var originalEggs = new Dictionary<string, ObjectData>(StringComparer.OrdinalIgnoreCase)
        {
            ["ModEgg_176"] = objects["ModEgg_176"]
        };

        var femaleEggs = new Dictionary<string, Dictionary<EggTypeEntries, string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Chicken"] = new()
            {
                [EggTypeEntries.Normal] = "ModEgg_176"
            }
        };

        var svc = new FertilizedEggsObjectEditService();

        svc.Apply(
            objects: objects,
            originalEggs: originalEggs,
            femaleEggs: femaleEggs,
            defaultFemales: new[] { "Chicken" },
            getFertilizedNameSuffix: () => "Fertilized",
            getFertilizedDescription: () => "Fertilized description",
            cloneEggsEnabled: true,
            includeMearasEggs: false
        );

        Assert.False(objects.ContainsKey("ModEgg_176_Fertilized"));
        Assert.False(femaleEggs["Chicken"].ContainsKey(EggTypeEntries.Fertilized));
    }

    [Fact]
    public void Apply_DoesNothing_WhenCloneDisabled()
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

        var originalEggs = new Dictionary<string, ObjectData>(StringComparer.OrdinalIgnoreCase)
        {
            ["ModEgg_176"] = objects["ModEgg_176"]
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
            originalEggs: originalEggs,
            femaleEggs: femaleEggs,
            defaultFemales: Array.Empty<string>(),
            getFertilizedNameSuffix: () => "Fertilized",
            getFertilizedDescription: () => "Fertilized description",
            cloneEggsEnabled: false,
            includeMearasEggs: false
        );

        Assert.False(objects.ContainsKey("ModEgg_176_Fertilized"));
        Assert.False(femaleEggs["CustomHen"].ContainsKey(EggTypeEntries.Fertilized));
    }
}
