using System;
using System.Collections.Generic;
using LetMeHatchThisEgg.Application.UseCases;
using StardewValley.GameData.Objects;

namespace LetMeHatchThisEgg.Application.Services;

/// <summary>
/// Application-layer service for Data/Objects edits.
/// Keeps branching/guard logic testable by injecting operations.
/// </summary>
public sealed class ObjectsAssetEditsService : IObjectsAssetEdits
{
    private readonly Func<int> _itemsToAddCount;
    private readonly Action<Dictionary<string, ObjectData>> _editClonedEggs;
    private readonly Action<Dictionary<string, ObjectData>> _editFertilizedEggs;
    private readonly Action<Dictionary<string, ObjectData>> _processTextures;

    public ObjectsAssetEditsService(
        Func<int> itemsToAddCount,
        Action<Dictionary<string, ObjectData>> editClonedEggs,
        Action<Dictionary<string, ObjectData>> editFertilizedEggs,
        Action<Dictionary<string, ObjectData>> processTextures)
    {
        _itemsToAddCount = itemsToAddCount ?? throw new ArgumentNullException(nameof(itemsToAddCount));
        _editClonedEggs = editClonedEggs ?? throw new ArgumentNullException(nameof(editClonedEggs));
        _editFertilizedEggs = editFertilizedEggs ?? throw new ArgumentNullException(nameof(editFertilizedEggs));
        _processTextures = processTextures ?? throw new ArgumentNullException(nameof(processTextures));
    }

    public bool TryAddClonedEggObjects(Dictionary<string, ObjectData> data, bool onlyFertilizedEggs)
    {
        if (_itemsToAddCount() < 1)
            return false;

        _editClonedEggs(data);

        if (!onlyFertilizedEggs)
            _processTextures(data);

        return true;
    }

    public bool TryAddFertilizedEggObjects(Dictionary<string, ObjectData> data)
    {
        if (_itemsToAddCount() < 1)
            return false;

        _editFertilizedEggs(data);
        _processTextures(data);
        return true;
    }
}
