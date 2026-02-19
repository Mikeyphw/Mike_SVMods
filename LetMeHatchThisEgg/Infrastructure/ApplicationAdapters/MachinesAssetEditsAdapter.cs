using System;
using System.Collections.Generic;
using LetMeHatchThisEgg.Application.UseCases;
using LetMeHatchThisEgg.GameAssetManagement;
using StardewValley.GameData.Machines;

namespace LetMeHatchThisEgg.Infrastructure.ApplicationAdapters;

/// <summary>Infrastructure adapter that performs Data/Machines edits using existing LMH managers.</summary>
public sealed class MachinesAssetEditsAdapter : IMachinesAssetEdits
{
    private readonly GameAssetManager _assets;

    public MachinesAssetEditsAdapter(GameAssetManager assets)
    {
        _assets = assets ?? throw new ArgumentNullException(nameof(assets));
    }

    public bool TryCloneMachineRules(Dictionary<string, MachineData> data, bool onlyFertilizedEggs, bool cloneMachineRules)
    {
        var shouldClone =
            (onlyFertilizedEggs && _assets.HasAddedFertilizedEggs)
            || (!onlyFertilizedEggs && _assets.HasAddedClonedItems && cloneMachineRules);

        if (!shouldClone)
            return false;

        _assets.MachineEditor.CloneMachineData(data);
        return true;
    }
}
