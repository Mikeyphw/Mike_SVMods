using LetMeHatchThisEgg.Application.UseCases;
using LetMeHatchThisEgg.Core.Cloning;
using Xunit;
using static LetMeHatchThisEgg.EnumsAndStrings.Enums;

namespace LetMeHatchThisEgg.Tests;

public sealed class CloneIdUseCaseTests
{
    [Fact]
    public void IsClonedEgg_WhenValidCloneEggId_ReturnsTrue()
    {
        var uc = new CloneIdUseCase();
        var id = "LetMeHatchThisEgg_#Chicken#176_ClonedNormalEgg";

        Assert.True(uc.IsClonedEgg(id));
        Assert.True(uc.IsClonedItem(id, CloneItemType.Egg));
    }

    [Fact]
    public void GetClonedItemId_UsesCoreFormatting()
    {
        var uc = new CloneIdUseCase();
        var id = uc.GetClonedItemId(
            modUniqueId: "LetMeHatchThisEgg",
            animalKey: "Duck",
            originalItemId: "442",
            type: CloneItemType.Egg,
            itemType: ItemType.Normal,
            fertilized: false
        );

        Assert.Equal("LetMeHatchThisEgg_#Duck#442_ClonedNormalEgg", id);
    }

    [Fact]
    public void TryParseCloneId_WhenValid_ReturnsParts()
    {
        var uc = new CloneIdUseCase();
        var id = "LetMeHatchThisEgg_#Cow#184_ClonedFertilizedEgg";

        Assert.True(uc.TryParseCloneId(id, out var parts));
        Assert.Equal(new CloneIdParts("Cow", "184", "ClonedFertilizedEgg"), parts);
    }
}
