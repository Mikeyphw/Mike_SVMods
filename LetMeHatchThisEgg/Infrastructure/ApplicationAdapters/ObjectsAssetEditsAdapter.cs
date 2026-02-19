using System;
using System.Collections.Generic;
using LetMeHatchThisEgg.Application.Services;
using LetMeHatchThisEgg.Application.UseCases;
using LetMeHatchThisEgg.GameAssetManagement;
using StardewValley.GameData.Objects;

namespace LetMeHatchThisEgg.Infrastructure.ApplicationAdapters;

/// <summary>Infrastructure adapter that wires Object edit operations into the application service.</summary>
public sealed class ObjectsAssetEditsAdapter : IObjectsAssetEdits
{
    private readonly ObjectsAssetEditsService _service;

    public ObjectsAssetEditsAdapter(GameAssetManager assets)
    {
        if (assets is null)
            throw new ArgumentNullException(nameof(assets));

        _service = new ObjectsAssetEditsService(
            itemsToAddCount: () => assets.ItemsToAdd.Count,
            editClonedEggs: data => assets.ItemManager.EditNewClonedEggs(data),
            editFertilizedEggs: data => assets.ItemManager.EditNewFertilizedEggs(data),
            processTextures: data => assets.TextureManager.ProcessClonedTexturesWithGrayColor(data));
    }

    public bool TryAddClonedEggObjects(Dictionary<string, ObjectData> data, bool onlyFertilizedEggs) =>
        _service.TryAddClonedEggObjects(data, onlyFertilizedEggs);

    public bool TryAddFertilizedEggObjects(Dictionary<string, ObjectData> data) =>
        _service.TryAddFertilizedEggObjects(data);
}
