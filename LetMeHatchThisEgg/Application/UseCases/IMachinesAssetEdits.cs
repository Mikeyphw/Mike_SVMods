using System.Collections.Generic;
using StardewValley.GameData.Machines;

namespace LetMeHatchThisEgg.Application.UseCases;

/// <summary>Application entrypoints for Data/Machines edits.</summary>
public interface IMachinesAssetEdits
{
    bool TryCloneMachineRules(Dictionary<string, MachineData> data, bool onlyFertilizedEggs, bool cloneMachineRules);
}
