using LetMeHatchThisEgg.Infrastructure.Cloning;
using StardewValley.GameData.FarmAnimals;
using Xunit;
using static LetMeHatchThisEgg.EnumsAndStrings.Enums;

namespace LetMeHatchThisEgg.Core.Tests;

public sealed class CloneTextureBookkeeperTests
{
    [Fact]
    public void ApplyTextures_WhenCurrentIsNull_UsesOriginalAndDoesNotRecord()
    {
        var bookkeeper = new CloneTextureBookkeeper();
        var cloneInfo = new LetMeHatchThisEgg.Models.ClonedAnimalInfo { FemaleKey = "WhiteChicken" };

        var clone = new FarmAnimalData
        {
            BabyTexture = null!,
            Texture = null!,
            HarvestedTexture = null!,
            ShopTexture = null!
        };

        var original = new FarmAnimalData
        {
            BabyTexture = "OrigBaby",
            Texture = "OrigAdult",
            HarvestedTexture = "OrigHarvested",
            ShopTexture = "OrigShop"
        };

        bookkeeper.ApplyTextures(
            contentPackId: "Test.Pack",
            cloneInfo: cloneInfo,
            cloneData: clone,
            originalData: original,
            placeholderData: new FarmAnimalData()
        );

        Assert.Equal("OrigBaby", clone.BabyTexture);
        Assert.Equal("OrigAdult", clone.Texture);
        Assert.Equal("OrigHarvested", clone.HarvestedTexture);
        Assert.Equal("OrigShop", clone.ShopTexture);

        Assert.False(cloneInfo.Textures.ContainsKey(TextureType.Baby));
        Assert.False(cloneInfo.Textures.ContainsKey(TextureType.Adult));
        Assert.False(cloneInfo.Textures.ContainsKey(TextureType.Harvested));
        Assert.False(cloneInfo.Textures.ContainsKey(TextureType.Shop));
    }

    [Fact]
    public void ApplyTextures_WhenCurrentIsCp_UsesContentPackPathAndRecords()
    {
        var bookkeeper = new CloneTextureBookkeeper();
        var cloneInfo = new LetMeHatchThisEgg.Models.ClonedAnimalInfo { FemaleKey = "VoidChicken" };

        var clone = new FarmAnimalData
        {
            BabyTexture = "cp",
            Texture = "CP",
            HarvestedTexture = "cp",
            ShopTexture = "cp"
        };

        var original = new FarmAnimalData
        {
            BabyTexture = "OrigBaby",
            Texture = "OrigAdult",
            HarvestedTexture = "OrigHarvested",
            ShopTexture = "OrigShop"
        };

        bookkeeper.ApplyTextures(
            contentPackId: "Author.Pack",
            cloneInfo: cloneInfo,
            cloneData: clone,
            originalData: original,
            placeholderData: new FarmAnimalData()
        );

        Assert.Equal("Animals/MaleClones/Author.Pack/VoidChicken/Baby", clone.BabyTexture);
        Assert.Equal("Animals/MaleClones/Author.Pack/VoidChicken/Adult", clone.Texture);
        Assert.Equal("Animals/MaleClones/Author.Pack/VoidChicken/Harvested", clone.HarvestedTexture);
        Assert.Equal("Animals/MaleClones/Author.Pack/VoidChicken/Shop", clone.ShopTexture);

        Assert.Equal(clone.BabyTexture, cloneInfo.Textures[TextureType.Baby]);
        Assert.Equal(clone.Texture, cloneInfo.Textures[TextureType.Adult]);
        Assert.Equal(clone.HarvestedTexture, cloneInfo.Textures[TextureType.Harvested]);
        Assert.Equal(clone.ShopTexture, cloneInfo.Textures[TextureType.Shop]);
    }

    [Fact]
    public void ApplyTextures_WhenCurrentIsCustom_UsesCustomAndDoesNotRecord()
    {
        var bookkeeper = new CloneTextureBookkeeper();
        var cloneInfo = new LetMeHatchThisEgg.Models.ClonedAnimalInfo { FemaleKey = "Duck" };

        var clone = new FarmAnimalData
        {
            BabyTexture = "CustomBaby",
            Texture = "CustomAdult",
            HarvestedTexture = "CustomHarvested",
            ShopTexture = "CustomShop"
        };

        var original = new FarmAnimalData
        {
            BabyTexture = "OrigBaby",
            Texture = "OrigAdult",
            HarvestedTexture = "OrigHarvested",
            ShopTexture = "OrigShop"
        };

        bookkeeper.ApplyTextures(
            contentPackId: "Any.Pack",
            cloneInfo: cloneInfo,
            cloneData: clone,
            originalData: original,
            placeholderData: new FarmAnimalData()
        );

        Assert.Equal("CustomBaby", clone.BabyTexture);
        Assert.Equal("CustomAdult", clone.Texture);
        Assert.Equal("CustomHarvested", clone.HarvestedTexture);
        Assert.Equal("CustomShop", clone.ShopTexture);

        Assert.Empty(cloneInfo.Textures);
    }
}
