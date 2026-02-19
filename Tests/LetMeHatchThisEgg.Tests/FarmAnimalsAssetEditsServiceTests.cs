using System.Collections.Generic;
using LetMeHatchThisEgg.Application.Services;
using StardewValley.GameData.FarmAnimals;
using Xunit;

namespace LetMeHatchThisEgg.Tests;

public sealed class FarmAnimalsAssetEditsServiceTests
{
    [Fact]
    public void TryApplyEdits_MutatesDictionary_WithExpectedInstance()
    {
        var expected = new FarmAnimalData();

        var service = new FarmAnimalsAssetEditsService(data =>
        {
            Assert.Empty(data);
            data["Chicken"] = expected;
        });

        var data = new Dictionary<string, FarmAnimalData>();
        Assert.True(service.TryApplyEdits(data));

        Assert.Single(data);
        Assert.True(data.TryGetValue("Chicken", out var actual));
        Assert.Same(expected, actual);
    }
}
