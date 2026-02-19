using System.Collections.Generic;
using LetMeHatchThisEgg.Application.Services;
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

        var data = new Dictionary<string, ObjectData>();
        Assert.False(service.TryAddClonedEggObjects(data, onlyFertilizedEggs: false));
        Assert.Empty(data);
    }

    [Fact]
    public void TryAddClonedEggObjects_WhenItems_AddsExpectedItem_AndRunsTexturesUnlessOnlyFertilized()
    {
        var expected = new ObjectData();
        var calledEdit = 0;
        var calledTex = 0;

        var service = new ObjectsAssetEditsService(
            itemsToAddCount: () => 1,
            editClonedEggs: d => { calledEdit++; d["X"] = expected; },
            editFertilizedEggs: _ => { },
            processTextures: _ => { calledTex++; });

        var data = new Dictionary<string, ObjectData>();

        Assert.True(service.TryAddClonedEggObjects(data, onlyFertilizedEggs: false));
        Assert.Equal(1, calledEdit);
        Assert.Equal(1, calledTex);
        Assert.Single(data);
        Assert.Same(expected, data["X"]);

        data.Clear();
        Assert.True(service.TryAddClonedEggObjects(data, onlyFertilizedEggs: true));
        Assert.Equal(2, calledEdit);
        Assert.Equal(1, calledTex);
    }

    [Fact]
    public void TryAddFertilizedEggObjects_WhenNoItems_ReturnsFalse_AndDoesNotMutate()
    {
        var service = new ObjectsAssetEditsService(
            itemsToAddCount: () => 0,
            editClonedEggs: _ => throw new Xunit.Sdk.XunitException("should not run"),
            editFertilizedEggs: _ => throw new Xunit.Sdk.XunitException("should not run"),
            processTextures: _ => throw new Xunit.Sdk.XunitException("should not run"));

        var data = new Dictionary<string, ObjectData>();
        Assert.False(service.TryAddFertilizedEggObjects(data));
        Assert.Empty(data);
    }

    [Fact]
    public void TryAddFertilizedEggObjects_WhenItems_AddsExpectedItem_AndRunsTextures()
    {
        var expected = new ObjectData();
        var calledEdit = 0;
        var calledTex = 0;

        var service = new ObjectsAssetEditsService(
            itemsToAddCount: () => 2,
            editClonedEggs: _ => { },
            editFertilizedEggs: d => { calledEdit++; d["F"] = expected; },
            processTextures: _ => { calledTex++; });

        var data = new Dictionary<string, ObjectData>();
        Assert.True(service.TryAddFertilizedEggObjects(data));

        Assert.Equal(1, calledEdit);
        Assert.Equal(1, calledTex);
        Assert.Single(data);
        Assert.Same(expected, data["F"]);
    }
}
