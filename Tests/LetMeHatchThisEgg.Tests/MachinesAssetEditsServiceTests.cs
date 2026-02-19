using System.Collections.Generic;
using LetMeHatchThisEgg.Application.Services;
using StardewValley.GameData.Machines;
using Xunit;

namespace LetMeHatchThisEgg.Tests;

public sealed class MachinesAssetEditsServiceTests
{
    [Fact]
    public void TryApplyEdits_MutatesDictionary_WithExpectedInstance()
    {
        var expected = new MachineData();

        var service = new MachinesAssetEditsService(data =>
        {
            Assert.Empty(data);
            data["Mayonnaise Machine"] = expected;
        });

        var data = new Dictionary<string, MachineData>();
        Assert.True(service.TryApplyEdits(data));

        Assert.Single(data);
        Assert.True(data.TryGetValue("Mayonnaise Machine", out var actual));
        Assert.Same(expected, actual);
    }
}
