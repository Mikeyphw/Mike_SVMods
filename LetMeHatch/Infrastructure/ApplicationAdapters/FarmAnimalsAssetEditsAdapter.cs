using System;
using System.Collections.Generic;
using LetMeHatch.Application.Services;
using LetMeHatch.Application.UseCases;
using LetMeHatch.Cloning;
using LetMeHatch.Config;
using LetMeHatch.GameAssetManagement;
using LetMeHatch.Infrastructure.Cloning;
using LetMeHatch.Infrastructure.GameData;
using StardewValley.GameData.FarmAnimals;
using static LetMeHatch.EnumsAndStrings.Enums;

namespace LetMeHatch.Infrastructure.ApplicationAdapters;

/// <summary>Infrastructure adapter that wires FarmAnimal edit operations into the application service.</summary>
public sealed class FarmAnimalsAssetEditsAdapter : IFarmAnimalsAssetEdits
{
    private readonly FarmAnimalsAssetEditsService _service;

    public FarmAnimalsAssetEditsAdapter(GameAssetManager assets)
    {
        if (assets is null)
            throw new ArgumentNullException(nameof(assets));

        _service = new FarmAnimalsAssetEditsService(
            addClonePlaceholders: data => AddClonePlaceholders(assets, data),
            applyCloneEdits: (data, stabilizeOrder) => ApplyCloneEdits(assets, data, stabilizeOrder),
            editEggListByName: (data, enabled) => EditEggListByName(assets, data, enabled),
            editBirthText: (data, enabled) => EditBirthText(assets, data, enabled),
            runBreedingSystem: (data, config, enabled) => RunBreedingSystem(assets, data, config, enabled),
            replaceOriginalItemsForClones: data => ReplaceOriginalItemsForClones(assets, data)
        );
    }

    public bool TryAddClonePlaceholders(Dictionary<string, FarmAnimalData> data) =>
        _service.TryAddClonePlaceholders(data);

    public bool TryApplyCloneEdits(Dictionary<string, FarmAnimalData> data, bool stabilizeOrder) =>
        _service.TryApplyCloneEdits(data, stabilizeOrder);

    public bool TryEditEggListByName(Dictionary<string, FarmAnimalData> data, bool enabled) =>
        _service.TryEditEggListByName(data, enabled);

    public bool TryEditBirthText(Dictionary<string, FarmAnimalData> data, bool enabled) =>
        _service.TryEditBirthText(data, enabled);

    public bool TryRunBreedingSystem(Dictionary<string, FarmAnimalData> data, ModConfig config, bool enabled) =>
        _service.TryRunBreedingSystem(data, config, enabled);

    public bool TryReplaceOriginalItemsForClones(Dictionary<string, FarmAnimalData> data) =>
        _service.TryReplaceOriginalItemsForClones(data);

    private static bool AddClonePlaceholders(GameAssetManager assets, Dictionary<string, FarmAnimalData> data)
    {
        if (assets.ClonedAnimalsInfoList.Count < 1)
            return false;

        bool changed = false;

        foreach (var cloneInfo in assets.ClonedAnimalsInfoList)
        {
            if (data.ContainsKey(cloneInfo.MaleKey))
                continue;

            data[cloneInfo.MaleKey] = AnimalCloner.GetFarmAnimalDataPlaceholder();
            changed = true;
        }

        return changed;
    }

    private static bool ApplyCloneEdits(GameAssetManager assets, Dictionary<string, FarmAnimalData> data, bool stabilizeOrder)
    {
        if (assets.ClonedAnimalsInfoList.Count < 1)
            return false;

        IFarmAnimalDataRepository repository = new DictionaryFarmAnimalDataRepository(data);
        if (stabilizeOrder)
        {
            repository = new OrderedFarmAnimalDataRepository(
                repository,
                FarmAnimalDataRepositoryOrderingMode.PreserveOriginalThenSortNewOrdinal);
        }

        assets.FarmAnimalCloneBatchApplier.Apply(
            repository,
            assets.ClonedAnimalsInfoList,
            new LoadedContentPackResolver(assets.LoadedContentPacks)
        );

        return true;
    }

    private static bool EditEggListByName(GameAssetManager assets, Dictionary<string, FarmAnimalData> data, bool enabled)
    {
        if (!enabled)
            return false;

        assets.EggListEditor.SetUpEggList(data);
        return true;
    }

    private static bool EditBirthText(GameAssetManager assets, Dictionary<string, FarmAnimalData> data, bool enabled)
    {
        if (!enabled)
            return false;

        assets.BirthTextEditor.EditBirthText(data);
        return true;
    }

    private static bool RunBreedingSystem(GameAssetManager assets, Dictionary<string, FarmAnimalData> data, ModConfig config, bool enabled)
    {
        if (!enabled)
            return false;

        assets.BreedingDataLoader.RunHatchingSystem(data, config, assets.BreedingConfigsByPack);
        return true;
    }

    private static bool ReplaceOriginalItemsForClones(GameAssetManager assets, Dictionary<string, FarmAnimalData> data)
    {
        if (assets.ItemsToAdd.Count < 1)
            return false;

        foreach (var cloneItemType in AnimalItemRelationsManager.GetCloneItemTypes())
            assets.ItemManager.ReplaceOriginalItemForClone(data, cloneItemType);

        return true;
    }

    private sealed class DictionaryFarmAnimalDataRepository : IFarmAnimalDataRepository
    {
        private readonly IDictionary<string, FarmAnimalData> _data;

        public DictionaryFarmAnimalDataRepository(IDictionary<string, FarmAnimalData> data)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
        }

        public IDictionary<string, FarmAnimalData> Data => _data;

        public FarmAnimalData Get(string key) => _data[key];

        public void Set(string key, FarmAnimalData value) => _data[key] = value;
    }
}
