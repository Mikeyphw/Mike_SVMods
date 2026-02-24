using System;
using System.Collections.Generic;
using LetMeHatch.Cloning;
using LetMeHatch.Core.Cloning;
using LetMeHatch.Infrastructure.GameData;
using LetMeHatch.Models;
using StardewValley.GameData.FarmAnimals;

namespace LetMeHatch.Infrastructure.Cloning;

/// <summary>Applies clone edits across all configured cloned animals for the FarmAnimals asset.</summary>
public sealed class FarmAnimalCloneBatchApplier
{
    private readonly IAnimalCloner _animalCloner;
    private readonly ICloneMappingService _cloneMappingService;

    public FarmAnimalCloneBatchApplier(IAnimalCloner animalCloner, ICloneMappingService cloneMappingService)
    {
        _animalCloner = animalCloner ?? throw new ArgumentNullException(nameof(animalCloner));
        _cloneMappingService = cloneMappingService ?? throw new ArgumentNullException(nameof(cloneMappingService));
    }

    public void Apply(
        IFarmAnimalDataRepository farmAnimalsRepository,
        IReadOnlyList<ClonedAnimalInfo> clonedAnimals,
        IContentPackResolver contentPacks
    )
    {
        if (farmAnimalsRepository is null)
            throw new ArgumentNullException(nameof(farmAnimalsRepository));
        if (clonedAnimals is null)
            throw new ArgumentNullException(nameof(clonedAnimals));
        if (contentPacks is null)
            throw new ArgumentNullException(nameof(contentPacks));

        IDictionary<string, FarmAnimalData> farmAnimals = farmAnimalsRepository.Data;

        foreach (var cloneInfo in clonedAnimals)
        {
            var contentPack = contentPacks.GetById(cloneInfo.ContentPackID);

            var femaleKey = cloneInfo.FemaleKey;
            var maleKey = cloneInfo.MaleKey;

            var original = farmAnimals[femaleKey];
            var clone = farmAnimals[maleKey];

            _animalCloner.EditClonedAnimal(
                contentPack: contentPack,
                cloneInfo: cloneInfo,
                originalAnimalData: original,
                currentCloneData: clone
            );

            _cloneMappingService.ApplyAlternatePurchaseRules(cloneInfo, farmAnimals);
        }

        if (farmAnimalsRepository is IFinalizableFarmAnimalDataRepository finalizable)
            finalizable.FinalizeEdits();
    }
}

