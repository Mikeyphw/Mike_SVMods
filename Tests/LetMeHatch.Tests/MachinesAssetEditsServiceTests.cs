using System.Collections.Generic;
using LetMeHatch.Application.Services;
using LetMeHatch.Tests.TestHelpers;
using StardewValley.GameData.Machines;
using Xunit;

namespace LetMeHatch.Tests;

public sealed class MachinesAssetEditsServiceTests
{
    [Fact]
    public void TryApplyEdits_WhenDelegateEdits_MutatesDictionary_AndMutatesSomeFields()
    {
        var expected = new MachineData();

        var service = new MachinesAssetEditsService(data =>
        {
            data["Mayonnaise Machine"] = expected;
            AssertEx.AssertSomePropertyMutation(expected, () => AssertEx.MutateSomeWritableProperties(expected));
        });

        var data = new Dictionary<string, MachineData>();
        Assert.True(service.TryApplyEdits(data));

        Assert.Single(data);
        Assert.True(data.TryGetValue("Mayonnaise Machine", out var actual));
        Assert.Same(expected, actual);
    }
}
