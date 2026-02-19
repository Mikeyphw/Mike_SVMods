using System;
using System.Collections.Generic;
using System.Linq;
using LetMeHatchThisEgg.Application.UseCases;
using LetMeHatchThisEgg.Breeding;
using LetMeHatchThisEgg.Cloning;
using LetMeHatchThisEgg.GameAssetManagement;
using LetMeHatchThisEgg.Infrastructure.Cloning;
using LetMeHatchThisEgg.Infrastructure.GameData;
using StardewValley.GameData.FarmAnimals;

namespace LetMeHatchThisEgg.Infrastructure.ApplicationAdapters;

/// <summary>Infrastructure adapter that performs Data/FarmAnimals edits using existing LMH managers.</summary>
public sealed class FarmAnimalsAssetEditsAdapter : IFarmAnimalsAssetEdits
{
    private readonly GameAssetManager _assets;

    public FarmAnimalsAssetEditsAdapter(GameAssetManager assets)
    {
        _assets = assets ?? throw new ArgumentNullException(nameof(assets));
    }

    public bool TryAddClonePlaceholders(Dictionary<string, FarmAnimalData> data)
    {
        if (_assets.ClonedAnimalsInfoList.Count == 0)
            return false;

        foreach (var maleKey in _assets.ClonedAnimalsInfoList.Select(x => x.MaleKey))
            data[maleKey] = AnimalCloner.GetFarmAnimalDataPlaceholder();

        return true;
    }

    public bool TryApplyCloneEdits(Dictionary<string, FarmAnimalData> data, bool stabilizeOrder)
    {
        if (_assets.ClonedAnimalsInfoList.Count == 0)
            return false;

        IFarmAnimalDataRepository repo = new FarmAnimalDataRepository(data);

        if (stabilizeOrder)
        {
            repo = new OrderedFarmAnimalDataRepository(
                inner: repo,
                orderingMode: FarmAnimalDataRepositoryOrderingMode.PreserveOriginalThenSortNewOrdinal
            );
        }

        var resolver = new LoadedContentPackResolver(_assets.LoadedContentPacks);

        _assets.FarmAnimalCloneBatchApplier.Apply(
            farmAnimalsRepository: repo,
            clonedAnimals: _assets.ClonedAnimalsInfoList,
            contentPacks: resolver
        );

        return true;
    }

    public bool TryEditEggListByName(Dictionary<string, FarmAnimalData> data, bool enabled)
    {
        if (!enabled)
            return false;

        _assets.EggListEditor.SetUpEggList(data);
        return true;
    }

    public bool TryEditBirthText(Dictionary<string, FarmAnimalData> data, bool enabled)
    {
        if (!enabled)
            return false;

        _assets.BirthTextEditor.EditBirthText(data);
        return true;
    }

    public bool TryRunBreedingSystem(Dictionary<string, FarmAnimalData> data, LetMeHatchThisEgg.Config.ModConfig config, bool enabled)
    {
        if (!enabled || !_assets.HasAddedMaleClones)
            return false;

        BreedingDataLoader.RunHatchingSystem(data, config, _assets.BreedingConfigsByPack);
        return true;
    }

    public bool TryReplaceOriginalItemsForClones(Dictionary<string, FarmAnimalData> data)
    {
        if (_assets.ItemsToAdd.Count < 1)
            return false;

        foreach (var cloneItemType in AnimalItemRelationsManager.GetCloneItemTypes())
            _assets.ItemManager.ReplaceOriginalItemForClone(data, cloneItemType);

        return true;
    }

    private sealed class FarmAnimalDataRepository : IFarmAnimalDataRepository
    {
        public FarmAnimalDataRepository(Dictionary<string, FarmAnimalData> inner)
        {
            Data = inner;
        }

        public IDictionary<string, FarmAnimalData> Data { get; }

        public FarmAnimalData Get(string key) => Data[key];

        public void Set(string key, FarmAnimalData value) => Data[key] = value;
    }
}
