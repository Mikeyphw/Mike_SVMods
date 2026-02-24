using System.Collections.Generic;
using LetMeHatch.Config;
using StardewValley.GameData.FarmAnimals;

namespace LetMeHatch.Application.UseCases;

/// <summary>Application entrypoints for Data/FarmAnimals edits.</summary>
public interface IFarmAnimalsAssetEdits
{
    bool TryAddClonePlaceholders(Dictionary<string, FarmAnimalData> data);
    bool TryApplyCloneEdits(Dictionary<string, FarmAnimalData> data, bool stabilizeOrder);
    bool TryEditEggListByName(Dictionary<string, FarmAnimalData> data, bool enabled);
    bool TryEditBirthText(Dictionary<string, FarmAnimalData> data, bool enabled);
    bool TryRunBreedingSystem(Dictionary<string, FarmAnimalData> data, ModConfig config, bool enabled);
    bool TryReplaceOriginalItemsForClones(Dictionary<string, FarmAnimalData> data);
}
