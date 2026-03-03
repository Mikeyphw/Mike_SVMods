using System.Collections.Generic;
using System.Linq;
using LetMeHatch.Core.Cloning;
using StardewValley.GameData.FarmAnimals;
using Xunit;
using static Mikew.NestCore.EnumsAndStrings.Strings.ItemIds;

namespace LetMeHatch.Core.Tests;

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
            HarvestType = FarmAnimalHarvestType.DropOvernight,
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
    public void ApplyPostLocalization_EnsuresCollections_AndOverwritesCustomFields()
    {
        var transformer = new FarmAnimalCloneTransformer();

        var original = new FarmAnimalData
        {
            CustomFields = new() { ["k"] = "v" }
        };

        var clone = new FarmAnimalData
        {
            CustomFields = new() { ["junk"] = "junk" },
            EggItemIds = null,
            ProduceItemIds = new(),
            DeluxeProduceItemIds = new()
        };

        transformer.ApplyPostLocalization(clone, original);

        Assert.NotNull(clone.CustomFields);
        Assert.True(clone.CustomFields.ContainsKey("k"));
        Assert.False(clone.CustomFields.ContainsKey("junk"));

        Assert.NotNull(clone.EggItemIds);
    }

    [Fact]
    public void ApplyPostLocalization_WhenCloneProduceListsEmpty_CopiesAndFiltersEggLikeItems()
    {
        var transformer = new FarmAnimalCloneTransformer();

        var vanillaEggId = AnimalProducts.VanillaEggs.First();
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
