using System;
using System.Collections.Generic;
using LetMeHatchThisEgg.Application.UseCases;
using StardewValley.GameData.FarmAnimals;

namespace LetMeHatchThisEgg.Application.Services;

/// <summary>
/// Application-layer service for Data/FarmAnimals edits.
/// </summary>
public sealed class FarmAnimalsAssetEditsService : IFarmAnimalsAssetEdits
{
    private readonly Action<Dictionary<string, FarmAnimalData>> _applyEdits;

    public FarmAnimalsAssetEditsService(Action<Dictionary<string, FarmAnimalData>> applyEdits)
    {
        _applyEdits = applyEdits ?? throw new ArgumentNullException(nameof(applyEdits));
    }

    public bool TryApplyEdits(Dictionary<string, FarmAnimalData> data)
    {
        _applyEdits(data);
        return true;
    }
}
