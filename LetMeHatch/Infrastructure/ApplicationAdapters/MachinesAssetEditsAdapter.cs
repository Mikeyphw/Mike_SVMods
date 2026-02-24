using System;
using System.Collections.Generic;
using LetMeHatch.Application.Services;
using LetMeHatch.Application.UseCases;
using LetMeHatch.GameAssetManagement;
using StardewValley.GameData.Machines;

namespace LetMeHatch.Infrastructure.ApplicationAdapters;

/// <summary>Infrastructure adapter that wires Machine edit operations into the application service.</summary>
public sealed class MachinesAssetEditsAdapter : IMachinesAssetEdits
{
    private readonly MachinesAssetEditsService _service;

    public MachinesAssetEditsAdapter(GameAssetManager assets)
    {
        if (assets is null)
            throw new ArgumentNullException(nameof(assets));

        _service = new MachinesAssetEditsService((data, _onlyFertilizedEggs, cloneMachineRules) =>
        {
            if (!cloneMachineRules || assets.ItemsToAdd.Count < 1)
                return false;

            assets.MachineEditor.CloneMachineData(data);
            return true;
        });
    }

    public bool TryCloneMachineRules(Dictionary<string, MachineData> data, bool onlyFertilizedEggs, bool cloneMachineRules) =>
        _service.TryCloneMachineRules(data, onlyFertilizedEggs, cloneMachineRules);
}
