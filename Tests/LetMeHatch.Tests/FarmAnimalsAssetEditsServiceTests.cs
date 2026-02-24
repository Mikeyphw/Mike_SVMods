using System.Collections.Generic;
using LetMeHatch.Application.Services;
using LetMeHatch.Tests.TestHelpers;
using StardewValley.GameData.FarmAnimals;
using Xunit;

namespace LetMeHatch.Tests;

public sealed class FarmAnimalsAssetEditsServiceTests
{
    [Fact]
    public void TryApplyEdits_WhenDelegateEdits_MutatesDictionary_AndMutatesSomeFields()
    {
        var expected = new FarmAnimalData();

        var service = new FarmAnimalsAssetEditsService(data =>
        {
            data["Chicken"] = expected;
            AssertEx.AssertSomePropertyMutation(expected, () => AssertEx.MutateSomeWritableProperties(expected));
        });

        var data = new Dictionary<string, FarmAnimalData>();
        Assert.True(service.TryApplyEdits(data));

        Assert.Single(data);
        Assert.True(data.TryGetValue("Chicken", out var actual));
        Assert.Same(expected, actual);
    }
}
