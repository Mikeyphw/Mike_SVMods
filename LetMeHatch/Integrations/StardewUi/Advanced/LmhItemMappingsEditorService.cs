using System;
using System.Collections.Generic;
using System.Linq;
using LetMeHatch.Config;
using LetMeHatch.Framework;
using static LetMeHatch.EnumsAndStrings.Enums;

namespace LetMeHatch.Integrations.StardewUi.Advanced;

public sealed class LmhItemMappingsEditorService
{
    public LmhItemMappingsEditorCatalog LoadCatalog(SavedItemsData savedItemsData, ModConfig config)
    {
        ArgumentNullException.ThrowIfNull(savedItemsData);
        ArgumentNullException.ThrowIfNull(config);

        var groups = savedItemsData.AnimalItemRelation
            .SelectMany(animal => animal.Value.Select(byType => new { animal.Key, CloneType = byType.Key, Items = byType.Value }))
            .Where(x => x.Items is not null)
            .GroupBy(x => x.CloneType)
            .OrderBy(x => x.Key.ToString(), StringComparer.OrdinalIgnoreCase)
            .Select(group => new LmhItemMappingsGroupRecord(
                group.Key,
                group.Key.ToString(),
                group.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
                    .Select(row =>
                    {
                        var configured = EnsureConfigMapping(config, row.Key, row.CloneType);
                        var items = row.Items ?? new Dictionary<ItemType, string>();
                        return new LmhItemMappingRecord(
                            row.Key,
                            configured.TryGetValue(ItemType.Normal, out var normal) && !string.IsNullOrWhiteSpace(normal)
                                ? normal
                                : items.TryGetValue(ItemType.Normal, out var fallbackNormal) ? fallbackNormal : string.Empty,
                            configured.TryGetValue(ItemType.Large, out var large) && !string.IsNullOrWhiteSpace(large)
                                ? large
                                : items.TryGetValue(ItemType.Large, out var fallbackLarge) ? fallbackLarge : string.Empty);
                    })
                    .ToList()))
            .ToList();

        return new LmhItemMappingsEditorCatalog(groups);
    }

    public LmhItemMappingsEditorDraft CreateDraft(LmhItemMappingsEditorCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        return new LmhItemMappingsEditorDraft(
            catalog.Groups.ToDictionary(
                group => group.CloneType,
                group => group.Mappings.ToDictionary(
                    row => row.AnimalKey,
                    row => new LmhItemMappingDraftRow(row.NormalItemId, row.LargeItemId),
                    StringComparer.OrdinalIgnoreCase)));
    }

    public void ApplyDraft(ModConfig config, SavedItemsData savedItemsData, LmhItemMappingsEditorDraft draft)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(savedItemsData);
        ArgumentNullException.ThrowIfNull(draft);

        foreach (var (cloneType, byAnimal) in draft.Mappings)
        {
            foreach (var (animalKey, row) in byAnimal)
            {
                var configMapping = EnsureConfigMapping(config, animalKey, cloneType);
                configMapping[ItemType.Normal] = row.NormalItemId ?? string.Empty;
                configMapping[ItemType.Large] = row.LargeItemId ?? string.Empty;

                if (!savedItemsData.AnimalItemRelation.TryGetValue(animalKey, out var byType))
                {
                    byType = new Dictionary<CloneItemType, Dictionary<ItemType, string>?>();
                    savedItemsData.AnimalItemRelation[animalKey] = byType;
                }

                byType[cloneType] ??= new Dictionary<ItemType, string>();
                byType[cloneType]![ItemType.Normal] = row.NormalItemId ?? string.Empty;
                byType[cloneType]![ItemType.Large] = row.LargeItemId ?? string.Empty;
            }
        }
    }

    private static Dictionary<ItemType, string> EnsureConfigMapping(ModConfig cfg, string animalKey, CloneItemType cloneType)
    {
        if (!cfg.ItemToClone.TryGetValue(animalKey, out var byCloneType) || byCloneType is null)
        {
            byCloneType = new Dictionary<CloneItemType, Dictionary<ItemType, string>>();
            cfg.ItemToClone[animalKey] = byCloneType;
        }

        if (!byCloneType.TryGetValue(cloneType, out var byItemType) || byItemType is null)
        {
            byItemType = new Dictionary<ItemType, string>
            {
                [ItemType.Normal] = string.Empty,
                [ItemType.Large] = string.Empty,
                [ItemType.FertilizedEgg] = string.Empty,
            };
            byCloneType[cloneType] = byItemType;
        }

        return byItemType;
    }
}
