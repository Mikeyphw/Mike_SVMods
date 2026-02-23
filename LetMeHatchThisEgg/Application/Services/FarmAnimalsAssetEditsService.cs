using System;
using System.Collections.Generic;
using LetMeHatchThisEgg.Application.UseCases;
using LetMeHatchThisEgg.Config;
using StardewValley.GameData.FarmAnimals;

namespace LetMeHatchThisEgg.Application.Services;

/// <summary>
/// Application-layer service for Data/FarmAnimals edits.
/// Keeps branching/guard logic testable by injecting operations.
/// </summary>
public sealed class FarmAnimalsAssetEditsService : IFarmAnimalsAssetEdits
{
    private readonly Func<Dictionary<string, FarmAnimalData>, bool> _addClonePlaceholders;
    private readonly Func<Dictionary<string, FarmAnimalData>, bool, bool> _applyCloneEdits;
    private readonly Func<Dictionary<string, FarmAnimalData>, bool, bool> _editEggListByName;
    private readonly Func<Dictionary<string, FarmAnimalData>, bool, bool> _editBirthText;
    private readonly Func<Dictionary<string, FarmAnimalData>, ModConfig, bool, bool> _runBreedingSystem;
    private readonly Func<Dictionary<string, FarmAnimalData>, bool> _replaceOriginalItemsForClones;

    public FarmAnimalsAssetEditsService(
        Func<Dictionary<string, FarmAnimalData>, bool> addClonePlaceholders,
        Func<Dictionary<string, FarmAnimalData>, bool, bool> applyCloneEdits,
        Func<Dictionary<string, FarmAnimalData>, bool, bool> editEggListByName,
        Func<Dictionary<string, FarmAnimalData>, bool, bool> editBirthText,
        Func<Dictionary<string, FarmAnimalData>, ModConfig, bool, bool> runBreedingSystem,
        Func<Dictionary<string, FarmAnimalData>, bool> replaceOriginalItemsForClones)
    {
        _addClonePlaceholders = addClonePlaceholders ?? throw new ArgumentNullException(nameof(addClonePlaceholders));
        _applyCloneEdits = applyCloneEdits ?? throw new ArgumentNullException(nameof(applyCloneEdits));
        _editEggListByName = editEggListByName ?? throw new ArgumentNullException(nameof(editEggListByName));
        _editBirthText = editBirthText ?? throw new ArgumentNullException(nameof(editBirthText));
        _runBreedingSystem = runBreedingSystem ?? throw new ArgumentNullException(nameof(runBreedingSystem));
        _replaceOriginalItemsForClones = replaceOriginalItemsForClones ?? throw new ArgumentNullException(nameof(replaceOriginalItemsForClones));
    }

    public bool TryAddClonePlaceholders(Dictionary<string, FarmAnimalData> data) =>
        _addClonePlaceholders(data);

    public bool TryApplyCloneEdits(Dictionary<string, FarmAnimalData> data, bool stabilizeOrder) =>
        _applyCloneEdits(data, stabilizeOrder);

    public bool TryEditEggListByName(Dictionary<string, FarmAnimalData> data, bool enabled) =>
        _editEggListByName(data, enabled);

    public bool TryEditBirthText(Dictionary<string, FarmAnimalData> data, bool enabled) =>
        _editBirthText(data, enabled);

    public bool TryRunBreedingSystem(Dictionary<string, FarmAnimalData> data, ModConfig config, bool enabled) =>
        _runBreedingSystem(data, config, enabled);

    public bool TryReplaceOriginalItemsForClones(Dictionary<string, FarmAnimalData> data) =>
        _replaceOriginalItemsForClones(data);
}
