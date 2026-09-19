using System.Collections.Generic;
using System.Linq;
using LetMeHatch.Core.Breeding;
using Xunit;

namespace LetMeHatch.Tests;

public sealed class CoopBreedingIndexTests
{
    [Fact]
    public void GetMaleQuantityForFemale_SumsMalesAcrossCoopsContainingFemaleType()
    {
        var adultCounts = new List<IDictionary<string, int>>
        {
            new Dictionary<string, int> { ["Chicken"] = 2, ["Rooster"] = 1 },
            new Dictionary<string, int> { ["Chicken"] = 1, ["Rooster"] = 0 },
            new Dictionary<string, int> { ["Duck"] = 1, ["Rooster"] = 5 }
        };

        var index = CoopBreedingIndex.FromAdultCounts(adultCounts);

        Assert.Equal(1, index.GetMaleQuantityForFemale("Rooster", "Chicken"));
        Assert.Equal(5, index.GetMaleQuantityForFemale("Rooster", "Duck"));
        Assert.Equal(0, index.GetMaleQuantityForFemale("Rooster", "Goat"));
    }
    [Fact]
    public void FromAdultCounts_NormalizesBoundaryInput_AndRemainsCaseInsensitive()
    {
        var adultCounts = new List<IDictionary<string, int>>
        {
            new Dictionary<string, int> { ["Chicken"] = 1, ["ROOSTER"] = 2, [""] = 9, ["Ignored"] = 0 },
            new Dictionary<string, int> { ["chicken"] = 1, ["rooster"] = 3 }
        };

        var index = CoopBreedingIndex.FromAdultCounts(adultCounts);

        Assert.Equal(5, index.GetMaleQuantityForFemale("Rooster", "CHICKEN"));
        Assert.DoesNotContain(index.GetAdultCountsByCoop().SelectMany(x => x.Keys), key => string.IsNullOrWhiteSpace(key));
        Assert.DoesNotContain(index.GetAdultCountsByCoop().SelectMany(x => x.Keys), key => string.Equals(key, "Ignored", System.StringComparison.OrdinalIgnoreCase));
    }

}
