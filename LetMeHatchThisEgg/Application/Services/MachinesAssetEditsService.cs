using System;
using System.Collections.Generic;
using LetMeHatchThisEgg.Application.UseCases;
using StardewValley.GameData.Machines;

namespace LetMeHatchThisEgg.Application.Services;

/// <summary>
/// Application-layer service for Data/Machines edits.
/// </summary>
public sealed class MachinesAssetEditsService : IMachinesAssetEdits
{
    private readonly Action<Dictionary<string, MachineData>> _applyEdits;

    public MachinesAssetEditsService(Action<Dictionary<string, MachineData>> applyEdits)
    {
        _applyEdits = applyEdits ?? throw new ArgumentNullException(nameof(applyEdits));
    }

    public bool TryApplyEdits(Dictionary<string, MachineData> data)
    {
        _applyEdits(data);
        return true;
    }
}
