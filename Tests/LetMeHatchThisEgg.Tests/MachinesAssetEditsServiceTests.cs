using System.Collections.Generic;
using LetMeHatchThisEgg.Application.Services;
using StardewValley.GameData.Machines;
using Xunit;

namespace LetMeHatchThisEgg.Tests;

public sealed class MachinesAssetEditsServiceTests
{
    [Fact]
    public void TryApplyEdits_CallsDelegate_AndCanMutateDictionary()
    {
        var called = false;

        var service = new MachinesAssetEditsService(data =>
        {
            called = true;
            data["Mayonnaise Machine"] = new MachineData();
        });

        var data = new Dictionary<string, MachineData>();
        Assert.True(service.TryApplyEdits(data));
        Assert.True(called);
        Assert.True(data.ContainsKey("Mayonnaise Machine"));
    }
}
