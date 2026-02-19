using LetMeHatchThisEgg.Core.Cloning;
using StardewValley.GameData.FarmAnimals;
using Xunit;

namespace LetMeHatchThisEgg.Core.Tests;

public sealed class FarmAnimalCloneNonLocalizedFieldPolicyTests
{
    [Fact]
    public void Apply_ReplacesPlaceholderValuesWithOriginal()
    {
        var original = new FarmAnimalData
        {
            House = "Coop",
            RequiredBuilding = "DeluxeCoop",
            UnlockCondition = "PLAYER_HAS_MAIL AnyMail",
            HarvestTool = "Shears",
            Sound = "cluck",
            BabySound = "peep"
        };

        var placeholder = new FarmAnimalData
        {
            House = null,
            RequiredBuilding = null,
            UnlockCondition = null,
            HarvestTool = null,
            Sound = null,
            BabySound = null
        };

        var clone = new FarmAnimalData
        {
            House = placeholder.House,
            RequiredBuilding = placeholder.RequiredBuilding,
            UnlockCondition = placeholder.UnlockCondition,
            HarvestTool = placeholder.HarvestTool,
            Sound = placeholder.Sound,
            BabySound = placeholder.BabySound
        };

        FarmAnimalCloneNonLocalizedFieldPolicy.Apply(clone, original, placeholder);

        Assert.Equal(original.House, clone.House);
        Assert.Equal(original.RequiredBuilding, clone.RequiredBuilding);
        Assert.Equal(original.UnlockCondition, clone.UnlockCondition);
        Assert.Equal(original.HarvestTool, clone.HarvestTool);
        Assert.Equal(original.Sound, clone.Sound);
        Assert.Equal(original.BabySound, clone.BabySound);
    }

    [Fact]
    public void Apply_LeavesNonPlaceholderValuesUntouched()
    {
        var original = new FarmAnimalData
        {
            House = "Coop",
            RequiredBuilding = "DeluxeCoop",
            UnlockCondition = "PLAYER_HAS_MAIL AnyMail",
            HarvestTool = "Shears",
            Sound = "cluck",
            BabySound = "peep"
        };

        var placeholder = new FarmAnimalData
        {
            House = null,
            RequiredBuilding = null,
            UnlockCondition = null,
            HarvestTool = null,
            Sound = null,
            BabySound = null
        };

        var clone = new FarmAnimalData
        {
            House = "Barn",
            RequiredBuilding = "BigBarn",
            UnlockCondition = "PLAYER_LEVEL Farming 5",
            HarvestTool = "MilkPail",
            Sound = "moo",
            BabySound = "mew"
        };

        FarmAnimalCloneNonLocalizedFieldPolicy.Apply(clone, original, placeholder);

        Assert.Equal("Barn", clone.House);
        Assert.Equal("BigBarn", clone.RequiredBuilding);
        Assert.Equal("PLAYER_LEVEL Farming 5", clone.UnlockCondition);
        Assert.Equal("MilkPail", clone.HarvestTool);
        Assert.Equal("moo", clone.Sound);
        Assert.Equal("mew", clone.BabySound);
    }
}
