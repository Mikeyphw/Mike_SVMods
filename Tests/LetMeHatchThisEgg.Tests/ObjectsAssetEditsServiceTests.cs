using System.Collections.Generic;
using LetMeHatchThisEgg.Application.Services;
using LetMeHatchThisEgg.Tests.TestHelpers;
using StardewValley.GameData.Objects;
using Xunit;

namespace LetMeHatchThisEgg.Tests;

public sealed class ObjectsAssetEditsServiceTests
{
    [Fact]
    public void TryAddClonedEggObjects_WhenNoItems_ReturnsFalse_AndDoesNotMutate()
    {
        var service = new ObjectsAssetEditsService(
            itemsToAddCount: () => 0,
            editClonedEggs: _ => throw new Xunit.Sdk.XunitException("should not run"),
            editFertilizedEggs: _ => throw new Xunit.Sdk.XunitException("should not run"),
            processTextures: _ => throw new Xunit.Sdk.XunitException("should not run"));

        var data = new Dictionary<string, ObjectData>
        {
            ["A"] = new ObjectData(),
        };

        AssertEx.AssertNoMutation(data, () => service.TryAddClonedEggObjects(data, onlyFertilizedEggs: false));
    }

    [Fact]
    public void TryAddClonedEggObjects_WhenItems_AddsExpectedItem_AndMutatesSomeFields()
    {
        var expected = new ObjectData();
        var calledEdit = 0;

        var service = new ObjectsAssetEditsService(
            itemsToAddCount: () => 1,
            editClonedEggs: d =>
            {
                calledEdit++;
                d["X"] = expected;

                // Simulate "real" edits: ensure a data object got field mutations.
                AssertEx.AssertSomePropertyMutation(expected, () => AssertEx.MutateSomeWritableProperties(expected));
            },
            editFertilizedEggs: _ => { },
            processTextures: _ => { });

        var data = new Dictionary<string, ObjectData>();

        Assert.True(service.TryAddClonedEggObjects(data, onlyFertilizedEggs: false));
        Assert.Equal(1, calledEdit);
        Assert.Single(data);
        Assert.Same(expected, data["X"]);
    }

    [Fact]
    public void TryAddFertilizedEggObjects_WhenNoItems_ReturnsFalse_AndDoesNotMutate()
    {
        var service = new ObjectsAssetEditsService(
            itemsToAddCount: () => 0,
            editClonedEggs: _ => throw new Xunit.Sdk.XunitException("should not run"),
            editFertilizedEggs: _ => throw new Xunit.Sdk.XunitException("should not run"),
            processTextures: _ => throw new Xunit.Sdk.XunitException("should not run"));

        var data = new Dictionary<string, ObjectData>
        {
            ["Z"] = new ObjectData(),
        };

        AssertEx.AssertNoMutation(data, () => service.TryAddFertilizedEggObjects(data));
    }

    [Fact]
    public void TryAddFertilizedEggObjects_WhenItems_AddsExpectedItem_AndMutatesSomeFields()
    {
        var expected = new ObjectData();
        var calledEdit = 0;

        var service = new ObjectsAssetEditsService(
            itemsToAddCount: () => 2,
            editClonedEggs: _ => { },
            editFertilizedEggs: d =>
            {
                calledEdit++;
                d["F"] = expected;
                AssertEx.AssertSomePropertyMutation(expected, () => AssertEx.MutateSomeWritableProperties(expected));
            },
            processTextures: _ => { });

        var data = new Dictionary<string, ObjectData>();
        Assert.True(service.TryAddFertilizedEggObjects(data));

        Assert.Equal(1, calledEdit);
        Assert.Single(data);
        Assert.Same(expected, data["F"]);
    }
}
