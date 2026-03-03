using System.Collections.Generic;
using LetMeHatch.Core.ClonePipeline;
using Xunit;
using static LetMeHatch.EnumsAndStrings.Enums;

namespace LetMeHatch.Core.Tests;

public sealed class CloneObjectDataBuilderTests
{
    [Fact]
    public void Build_CopiesWhitelistFields_AndAppliesOverrides()
    {
        var spec = new CloneSpec(
            AnimalKey: "Chicken",
            OriginalItemId: "176",
            CloneItemId: "Mikew.letmehatch_#Chicken#176_ClonedNormalEgg",
            CloneItemType: CloneItemType.Egg,
            ItemType: ItemType.Normal,
            IsFertilized: false,
            ColorSeedKey: "seed"
        );

        var source = new CloneObjectDataSource(
            Name: "Egg",
            Description: "A regular egg.",
            Price: 50,
            Edibility: -300,
            Category: -5,
            ContextTags: new[] { "egg", "color_white" },
            CustomFields: new Dictionary<string, string> { ["originalEgg"] = "176" },
            TextureAssetName: "Maps\\springobjects",
            SpriteIndex: 176,
            DisplayName: "Egg",
            Type: "Egg",
            ArtifactSpotChances: new Dictionary<string, float> { ["(O)114"] = 0.5f }
        );

        var overrides = new CloneObjectDataOverrides(
            Name: "Chicken Egg",
            DisplayName: "Egg: Chicken",
            Price: 65,
            AdditionalContextTags: new[] { "cloned_egg" },
            AdditionalCustomFields: new Dictionary<string, string> { ["animal"] = "Chicken" }
        );

        var builder = new CloneObjectDataBuilder();
        var result = builder.Build(new CloneObjectDataRequest(spec, source, overrides));

        Assert.Equal(spec.CloneItemId, result.Id);
        Assert.Equal("Chicken Egg", result.Name);
        Assert.Equal("Egg: Chicken", result.DisplayName);
        Assert.Equal(source.Description, result.Description);
        Assert.Equal(65, result.Price);
        Assert.Equal(source.Edibility, result.Edibility);
        Assert.Equal(source.Category, result.Category);
        Assert.Equal(source.Type, result.Type);
        Assert.NotNull(result.ArtifactSpotChances);
        Assert.Equal(0.5f, result.ArtifactSpotChances!["(O)114"]);

        Assert.Contains("cloned_egg", result.ContextTags);
        Assert.Contains("egg", result.ContextTags);
        Assert.Equal("Chicken", result.CustomFields["animal"]);
        Assert.Equal("176", result.CustomFields["originalEgg"]);
        Assert.Equal(source.TextureAssetName, result.TextureAssetName);
        Assert.Equal(source.SpriteIndex, result.SpriteIndex);
    }

    [Fact]
    public void Build_MergesTagsDistinct_CaseInsensitive()
    {
        var spec = new CloneSpec("A", "1", "id", CloneItemType.Egg, ItemType.Normal, false, "seed");
        var source = new CloneObjectDataSource("n", "d", 1, 1, 1, ContextTags: new[] { "Egg", "egg" });
        var overrides = new CloneObjectDataOverrides(AdditionalContextTags: new[] { "EGG", "cloned" });

        var builder = new CloneObjectDataBuilder();
        var result = builder.Build(new CloneObjectDataRequest(spec, source, overrides));

        Assert.Equal(2, result.ContextTags.Count);
        Assert.Contains("egg", result.ContextTags, System.StringComparer.OrdinalIgnoreCase);
        Assert.Contains("cloned", result.ContextTags, System.StringComparer.OrdinalIgnoreCase);
    }
}
