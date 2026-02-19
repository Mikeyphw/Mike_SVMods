using System.Collections.Generic;
using LetMeHatchThisEgg.Application.Services;
using StardewValley.GameData.FarmAnimals;
using Xunit;

namespace LetMeHatchThisEgg.Tests;

public sealed class FarmAnimalsAssetEditsServiceTests
{
    [Fact]
    public void TryApplyEdits_CallsDelegate_AndCanMutateDictionary()
    {
        var called = false;

        var service = new FarmAnimalsAssetEditsService(data =>
        {
            called = true;
            data["Chicken"] = new FarmAnimalData();
        });

        var data = new Dictionary<string, FarmAnimalData>();
        Assert.True(service.TryApplyEdits(data));
        Assert.True(called);
        Assert.True(data.ContainsKey("Chicken"));
    }
}
