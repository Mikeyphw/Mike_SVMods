using LetMeHatchThisEgg.Core.Cloning;
using static LetMeHatchThisEgg.EnumsAndStrings.Enums;
using Xunit;

namespace LetMeHatchThisEgg.Tests;

public sealed class CloneIdServiceTests
{
    [Fact]
    public void GetClonedItemId_Egg_Normal_Unfertilized_FormatsCorrectly()
    {
        var id = CloneIdService.GetClonedItemId("Mike.Mod", "Chicken", "176", CloneItemType.Egg, ItemType.Normal, isFertilized: false);
        Assert.Equal("Mike.Mod_#Chicken#176_ClonedNormalEgg", id);
        Assert.True(CloneIdService.IsClonedEgg(id));
        Assert.True(CloneIdService.IsClonedItem(id, CloneItemType.Egg, fertilized: false));
    }

    [Fact]
    public void GetClonedItemId_Egg_Fertilized_FormatsCorrectly()
    {
        var id = CloneIdService.GetClonedItemId("Mike.Mod", "Duck", "442", CloneItemType.Egg, ItemType.Normal, isFertilized: true);
        Assert.Equal("Mike.Mod_#Duck#442_ClonedFertilizedEgg", id);
        Assert.True(CloneIdService.IsClonedItem(id, CloneItemType.Egg, fertilized: true));
    }

    [Theory]
    [InlineData(CloneItemType.Milk, "ClonedNormalMilk")]
    [InlineData(CloneItemType.Misc, "ClonedNormalMisc")]
    public void GetClonedItemId_OtherTypes_FormatsCorrectly(CloneItemType type, string suffix)
    {
        var id = CloneIdService.GetClonedItemId("Mike.Mod", "Cow", "184", type, ItemType.Normal);
        Assert.Equal($"Mike.Mod_#Cow#184_{suffix}", id);
        Assert.True(CloneIdService.IsClonedItem(id, type));
    }

    [Fact]
    public void TryParseCloneId_ParsesParts()
    {
        var id = "Mike.Mod_#Cow#184_ClonedNormalMilk";
        var ok = CloneIdService.TryParseCloneId(id, out var animalKey, out var originalId, out var suffix);

        Assert.True(ok);
        Assert.Equal("Cow", animalKey);
        Assert.Equal("184", originalId);
        Assert.Equal("ClonedNormalMilk", suffix);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-clone")]
    public void IsClonedItem_FalseForInvalid(string? value)
    {
        Assert.False(CloneIdService.IsClonedItem(value ?? string.Empty));
    }
}
