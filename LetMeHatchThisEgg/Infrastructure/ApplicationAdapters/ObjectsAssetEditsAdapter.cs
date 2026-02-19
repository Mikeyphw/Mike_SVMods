using System;
using System.Collections.Generic;
using LetMeHatchThisEgg.Application.UseCases;
using LetMeHatchThisEgg.GameAssetManagement;
using StardewValley.GameData.Objects;

namespace LetMeHatchThisEgg.Infrastructure.ApplicationAdapters;

/// <summary>Infrastructure adapter that performs Data/Objects edits using existing LMH managers.</summary>
public sealed class ObjectsAssetEditsAdapter : IObjectsAssetEdits
{
    private readonly GameAssetManager _assets;

    public ObjectsAssetEditsAdapter(GameAssetManager assets)
    {
        _assets = assets ?? throw new ArgumentNullException(nameof(assets));
    }

    public bool TryAddClonedEggObjects(Dictionary<string, ObjectData> data, bool onlyFertilizedEggs)
    {
        if (_assets.ItemsToAdd.Count < 1)
            return false;

        _assets.ItemManager.EditNewClonedEggs(data);

        if (!onlyFertilizedEggs)
            _assets.TextureManager.ProcessClonedTexturesWithGrayColor(data);

        return true;
    }

    public bool TryAddFertilizedEggObjects(Dictionary<string, ObjectData> data)
    {
        if (_assets.ItemsToAdd.Count < 1)
            return false;

        _assets.ItemManager.EditNewFertilizedEggs(data);
        _assets.TextureManager.ProcessClonedTexturesWithGrayColor(data);
        return true;
    }
}
