using System;
using System.Collections.Generic;
using LetMeHatchThisEgg.Application.Services;
using LetMeHatchThisEgg.Application.UseCases;
using LetMeHatchThisEgg.GameAssetManagement;
using StardewValley.GameData.FarmAnimals;

namespace LetMeHatchThisEgg.Infrastructure.ApplicationAdapters;

/// <summary>Infrastructure adapter that wires FarmAnimal edit operations into the application service.</summary>
public sealed class FarmAnimalsAssetEditsAdapter : IFarmAnimalsAssetEdits
{
    private readonly FarmAnimalsAssetEditsService _service;

    public FarmAnimalsAssetEditsAdapter(GameAssetManager assets)
    {
        if (assets is null)
            throw new ArgumentNullException(nameof(assets));

        _service = new FarmAnimalsAssetEditsService(data => assets.FarmAnimalManager.EditFarmAnimalData(data));
    }

    public bool TryApplyEdits(Dictionary<string, FarmAnimalData> data) => _service.TryApplyEdits(data);
}
