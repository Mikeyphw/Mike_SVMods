using System;
using System.Collections.Generic;
using LetMeHatchThisEgg.Application.Services;
using LetMeHatchThisEgg.Application.UseCases;
using LetMeHatchThisEgg.GameAssetManagement;
using StardewValley.GameData.Machines;

namespace LetMeHatchThisEgg.Infrastructure.ApplicationAdapters;

/// <summary>Infrastructure adapter that wires Machine edit operations into the application service.</summary>
public sealed class MachinesAssetEditsAdapter : IMachinesAssetEdits
{
    private readonly MachinesAssetEditsService _service;

    public MachinesAssetEditsAdapter(GameAssetManager assets)
    {
        if (assets is null)
            throw new ArgumentNullException(nameof(assets));

        _service = new MachinesAssetEditsService(data => assets.MachineManager.EditMachineData(data));
    }

    public bool TryApplyEdits(Dictionary<string, MachineData> data) => _service.TryApplyEdits(data);
}
