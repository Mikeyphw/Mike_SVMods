using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using LetMeHatch.GameAssetManagement;
using LetMeHatch.Infrastructure.ApplicationAdapters;
using StardewValley.GameData.FarmAnimals;
using StardewValley.GameData.Objects;
using Xunit;

namespace LetMeHatch.Tests;

public sealed class AdapterGatingTests
{
    [Fact]
    public void ObjectsAdapter_WhenNoItemsToAdd_ReturnsFalseWithoutThrowing()
    {
        var assets = CreateAssets(itemsToAddCount: 0, clonedAnimalsCount: 0);
        var adapter = new ObjectsAssetEditsAdapter(assets);

        var data = new Dictionary<string, ObjectData>();
        var ok = adapter.TryAddClonedEggObjects(data, onlyFertilizedEggs: true);

        Assert.False(ok);
    }

    [Fact]
    public void FarmAnimalsAdapter_WhenNoClones_ReturnsFalseWithoutThrowing()
    {
        var assets = CreateAssets(itemsToAddCount: 0, clonedAnimalsCount: 0);
        var adapter = new FarmAnimalsAssetEditsAdapter(assets);

        var data = new Dictionary<string, FarmAnimalData>();
        Assert.False(adapter.TryAddClonePlaceholders(data));
        Assert.False(adapter.TryApplyCloneEdits(data, stabilizeOrder: false));
    }

    private static GameAssetManager CreateAssets(int itemsToAddCount, int clonedAnimalsCount)
    {
        var obj = (GameAssetManager)FormatterServices.GetUninitializedObject(typeof(GameAssetManager));

        SetAutoProperty(obj, "ItemsToAdd", itemsToAddCount == 0
            ? new Dictionary<string, Dictionary<LetMeHatch.EnumsAndStrings.Enums.CloneItemType, Dictionary<string, ObjectData>>>()
            : new Dictionary<string, Dictionary<LetMeHatch.EnumsAndStrings.Enums.CloneItemType, Dictionary<string, ObjectData>>> { { "k", new() } });

        SetAutoProperty(obj, "ClonedAnimalsInfoList", clonedAnimalsCount == 0
            ? new List<LetMeHatch.Models.ClonedAnimalInfo>()
            : new List<LetMeHatch.Models.ClonedAnimalInfo>());

        return obj;
    }

    private static void SetAutoProperty<T>(object target, string propertyName, T value)
    {
        var field = target.GetType().GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        if (field is null)
            throw new InvalidOperationException($"Backing field not found for {propertyName}.");

        field.SetValue(target, value);
    }
}
