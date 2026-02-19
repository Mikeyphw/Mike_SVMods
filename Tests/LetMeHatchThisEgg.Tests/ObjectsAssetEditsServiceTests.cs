using System.Collections.Generic;
using LetMeHatchThisEgg.Application.Services;
using StardewValley.GameData.Objects;
using Xunit;

namespace LetMeHatchThisEgg.Tests;

public sealed class ObjectsAssetEditsServiceTests
{
    [Fact]
    public void TryAddClonedEggObjects_WhenNoItems_ReturnsFalse()
    {
        var service = new ObjectsAssetEditsService(
            itemsToAddCount: () => 0,
            editClonedEggs: _ => throw new Xunit.Sdk.XunitException("should not run"),
            editFertilizedEggs: _ => throw new Xunit.Sdk.XunitException("should not run"),
            processTextures: _ => throw new Xunit.Sdk.XunitException("should not run"));

        var data = new Dictionary<string, ObjectData>();
        Assert.False(service.TryAddClonedEggObjects(data, onlyFertilizedEggs: false));
    }

    [Fact]
    public void TryAddClonedEggObjects_WhenItems_AddsAndOptionallyTextures()
    {
        var calledEdit = false;
        var calledTex = false;

        var service = new ObjectsAssetEditsService(
            itemsToAddCount: () => 1,
            editClonedEggs: d => { calledEdit = true; d["X"] = new ObjectData(); },
            editFertilizedEggs: _ => { },
            processTextures: _ => { calledTex = true; });

        var data = new Dictionary<string, ObjectData>();
        Assert.True(service.TryAddClonedEggObjects(data, onlyFertilizedEggs: false));
        Assert.True(calledEdit);
        Assert.True(calledTex);
        Assert.True(data.ContainsKey("X"));
    }

    [Fact]
    public void TryAddClonedEggObjects_WhenOnlyFertilized_SkipsTextures()
    {
        var calledTex = false;

        var service = new ObjectsAssetEditsService(
            itemsToAddCount: () => 1,
            editClonedEggs: _ => { },
            editFertilizedEggs: _ => { },
            processTextures: _ => { calledTex = true; });

        var data = new Dictionary<string, ObjectData>();
        Assert.True(service.TryAddClonedEggObjects(data, onlyFertilizedEggs: true));
        Assert.False(calledTex);
    }
}
