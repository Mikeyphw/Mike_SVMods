using System.Collections.Generic;
using System.Linq;
using LetMeHatchThisEgg.Core.Cloning;
using Mikew.BirdsOfAFeather.EnumsAndStrings;
using StardewValley.GameData.FarmAnimals;
using Xunit;

namespace LetMeHatchThisEgg.Core.Tests;

public sealed class FarmAnimalCloneTransformerTests
{
    private static FarmAnimalProduce Produce(string itemId)
    {
        return new FarmAnimalProduce { Id = itemId, ItemId = itemId };
    }

    [Fact]
    public void ApplyPreLocalization_SetsInvariantDefaults_AndClearsControlledFields()
    {
        var transformer = new FarmAnimalCloneTransformer();
        var clone = new FarmAnimalData
        {
            Gender = FarmAnimalGender.Female,
            HarvestType = FarmAnimalHarvestType.Grab,
            EggItemIds = new() { "999" },
            AlternatePurchaseTypes = new()
        };

        transformer.ApplyPreLocalization(clone);

        Assert.Equal(FarmAnimalGender.Male, clone.Gender);
        Assert.Equal(FarmAnimalHarvestType.DropOvernight, clone.HarvestType);
        Assert.Null(clone.EggItemIds);
        Assert.Null(clone.AlternatePurchaseTypes);
    }

    [Fact]
    public void ApplyPostLocalization_CopiesDefaults_AndEnsuresCollections()
    {
        var transformer = new FarmAnimalCloneTransformer();

        var original = new FarmAnimalData
        {
            ShadowWhenBabySwims = true,
            ShadowWhenBaby = false,
            ShadowWhenAdultSwims = true,
            ShadowWhenAdult = false,
            Skins = new() { "skinA" },
            StatToIncrementOnProduce = new() { "statA" },
            CustomFields = new() { ["k"] = "v" }
        };

        var clone = new FarmAnimalData
        {
            ShadowWhenBabySwims = null,
            ShadowWhenBaby = null,
            ShadowWhenAdultSwims = null,
            ShadowWhenAdult = null,
            Skins = null,
            StatToIncrementOnProduce = null,
            CustomFields = new() { ["junk"] = "junk" },
            EggItemIds = null,
            ProduceItemIds = new(),
            DeluxeProduceItemIds = new()
        };

        transformer.ApplyPostLocalization(clone, original);

        Assert.Equal(original.ShadowWhenBabySwims, clone.ShadowWhenBabySwims);
        Assert.Equal(original.ShadowWhenBaby, clone.ShadowWhenBaby);
        Assert.Equal(original.ShadowWhenAdultSwims, clone.ShadowWhenAdultSwims);
        Assert.Equal(original.ShadowWhenAdult, clone.ShadowWhenAdult);

        Assert.NotNull(clone.Skins);
        Assert.Contains("skinA", clone.Skins);
        Assert.NotNull(clone.StatToIncrementOnProduce);
        Assert.Contains("statA", clone.StatToIncrementOnProduce);

        // Preserve behavior: CustomFields overwrites clone junk.
        Assert.NotNull(clone.CustomFields);
        Assert.True(clone.CustomFields.ContainsKey("k"));
        Assert.False(clone.CustomFields.ContainsKey("junk"));

        Assert.NotNull(clone.EggItemIds);
    }

    [Fact]
    public void ApplyPostLocalization_WhenCloneProduceListsEmpty_CopiesAndFiltersEggLikeItems()
    {
        var transformer = new FarmAnimalCloneTransformer();

        var vanillaEggId = Strings.AnimalProducts.VanillaEggs.First();
        var regexEggId = "my_egg_item";
        var predicateEggId = "categoryEgg";
        var nonEggId = "nonEggItem";

        var original = new FarmAnimalData
        {
            ProduceItemIds = new()
            {
                Produce(vanillaEggId),
                Produce(regexEggId),
                Produce(predicateEggId),
                Produce(nonEggId),
            },
            DeluxeProduceItemIds = new()
            {
                Produce(vanillaEggId),
                Produce(nonEggId),
            },
        };

        var clone = new FarmAnimalData
        {
            ProduceItemIds = new(),
            DeluxeProduceItemIds = new(),
        };

        transformer.ApplyPostLocalization(
            clone: clone,
            original: original,
            isEggCategoryObjectId: id => id == predicateEggId
        );

        Assert.DoesNotContain(clone.ProduceItemIds, p => p.ItemId == vanillaEggId);
        Assert.DoesNotContain(clone.ProduceItemIds, p => p.ItemId == regexEggId);
        Assert.DoesNotContain(clone.ProduceItemIds, p => p.ItemId == predicateEggId);
        Assert.Contains(clone.ProduceItemIds, p => p.ItemId == nonEggId);

        Assert.DoesNotContain(clone.DeluxeProduceItemIds, p => p.ItemId == vanillaEggId);
        Assert.Contains(clone.DeluxeProduceItemIds, p => p.ItemId == nonEggId);
    }
}
