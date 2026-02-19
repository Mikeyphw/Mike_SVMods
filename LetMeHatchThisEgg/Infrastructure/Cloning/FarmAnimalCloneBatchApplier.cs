using System;
using System.Collections.Generic;
using System.Linq;
using LetMeHatchThisEgg.Cloning;
using LetMeHatchThisEgg.Core.Cloning;
using LetMeHatchThisEgg.Models;
using StardewModdingAPI;
using StardewValley.GameData.FarmAnimals;

namespace LetMeHatchThisEgg.Infrastructure.Cloning;

/// <summary>
/// Applies clone edits across all configured cloned animals for the FarmAnimals asset.
/// </summary>
public sealed class FarmAnimalCloneBatchApplier
{
    private readonly AnimalCloner _animalCloner;
    private readonly CloneMappingService _cloneMappingService;

    public FarmAnimalCloneBatchApplier(
        AnimalCloner animalCloner,
        CloneMappingService cloneMappingService
    )
    {
        _animalCloner = animalCloner ?? throw new ArgumentNullException(nameof(animalCloner));
        _cloneMappingService = cloneMappingService ?? throw new ArgumentNullException(nameof(cloneMappingService));
    }

    public void Apply(
        IAssetData asset,
        IReadOnlyList<ClonedAnimalInfo> clonedAnimals,
        IReadOnlyList<IContentPack> loadedContentPacks
    )
    {
        if (asset is null)
        {
            throw new ArgumentNullException(nameof(asset));
        }

        if (clonedAnimals is null)
        {
            throw new ArgumentNullException(nameof(clonedAnimals));
        }

        if (loadedContentPacks is null)
        {
            throw new ArgumentNullException(nameof(loadedContentPacks));
        }

        var farmAnimals = asset.AsDictionary<string, FarmAnimalData>().Data;

        foreach (var cloneInfo in clonedAnimals)
        {
            var contentPack = loadedContentPacks.First(cp => cp.Manifest.UniqueID == cloneInfo.ContentPackID);

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
    }
}
