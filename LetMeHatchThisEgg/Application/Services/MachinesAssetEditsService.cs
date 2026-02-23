using System;
using System.Collections.Generic;
using LetMeHatchThisEgg.Application.UseCases;
using StardewValley.GameData.Machines;

namespace LetMeHatchThisEgg.Application.Services;

/// <summary>
/// Application-layer service for Data/Machines edits.
/// Keeps branching/guard logic testable by injecting operations.
/// </summary>
public sealed class MachinesAssetEditsService : IMachinesAssetEdits
{
    private readonly Func<Dictionary<string, MachineData>, bool, bool, bool> _cloneMachineRules;

    public MachinesAssetEditsService(Func<Dictionary<string, MachineData>, bool, bool, bool> cloneMachineRules)
    {
        _cloneMachineRules = cloneMachineRules ?? throw new ArgumentNullException(nameof(cloneMachineRules));
    }

    public bool TryCloneMachineRules(Dictionary<string, MachineData> data, bool onlyFertilizedEggs, bool cloneMachineRules) =>
        _cloneMachineRules(data, onlyFertilizedEggs, cloneMachineRules);
}
