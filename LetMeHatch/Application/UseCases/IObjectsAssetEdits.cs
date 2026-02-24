using System.Collections.Generic;
using StardewValley.GameData.Objects;

namespace LetMeHatch.Application.UseCases;

/// <summary>Application entrypoints for Data/Objects edits.</summary>
public interface IObjectsAssetEdits
{
    bool TryAddClonedEggObjects(Dictionary<string, ObjectData> data, bool onlyFertilizedEggs);
    bool TryAddFertilizedEggObjects(Dictionary<string, ObjectData> data);
}
