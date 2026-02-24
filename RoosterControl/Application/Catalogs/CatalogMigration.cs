using System;
using System.Collections.Generic;

namespace RoosterControl.Application.Catalogs;

internal static class CatalogMigrator
{
    internal static void MergeMissing(CatalogBundle target, CatalogBundle defaults)
    {
        if (target is null || defaults is null)
            return;

        MergeRoosters(target.Roosters, defaults.Roosters);
        MergeProduce(target.Produce, defaults.Produce);
        MergeHatching(target.Hatching, defaults.Hatching);
    }

    private static void MergeRoosters(RoosterCatalog target, RoosterCatalog defaults)
    {
        target ??= new RoosterCatalog();
        defaults ??= new RoosterCatalog();

        target.Roosters ??= new Dictionary<string, RoosterCatalogEntry>(StringComparer.OrdinalIgnoreCase);

        foreach (var (key, def) in defaults.Roosters ?? new Dictionary<string, RoosterCatalogEntry>())
        {
            if (string.IsNullOrWhiteSpace(key) || def is null)
                continue;

            if (!target.Roosters.TryGetValue(key, out var existing) || existing is null)
            {
                target.Roosters[key] = def;
                continue;
            }

            existing.FarmAnimalKey ??= def.FarmAnimalKey;
            existing.DefaultFemaleKey ??= def.DefaultFemaleKey;
            existing.AdditionalFemaleKeys ??= def.AdditionalFemaleKeys ?? new List<string>();
            existing.Eggs ??= def.Eggs ?? new EggCatalog();
        }
    }

    private static void MergeProduce(ProduceCatalog target, ProduceCatalog defaults)
    {
        target ??= new ProduceCatalog();
        defaults ??= new ProduceCatalog();

        target.RandomGroups ??= new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, defList) in defaults.RandomGroups ?? new Dictionary<string, List<string>>())
        {
            if (string.IsNullOrWhiteSpace(key))
                continue;

            if (!target.RandomGroups.ContainsKey(key))
                target.RandomGroups[key] = defList ?? new List<string>();
        }

        target.Roosters ??= new Dictionary<string, ProduceCatalogEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, def) in defaults.Roosters ?? new Dictionary<string, ProduceCatalogEntry>())
        {
            if (string.IsNullOrWhiteSpace(key) || def is null)
                continue;

            if (!target.Roosters.TryGetValue(key, out var existing) || existing is null)
            {
                target.Roosters[key] = def;
                continue;
            }

            existing.BlueEggChoice ??= def.BlueEggChoice;

            existing.Produce ??= def.Produce ?? new List<ProduceEntryDto>();
            existing.Deluxe ??= def.Deluxe ?? new List<ProduceEntryDto>();
        }
    }

    private static void MergeHatching(HatchingCatalog target, HatchingCatalog defaults)
    {
        target ??= new HatchingCatalog();
        defaults ??= new HatchingCatalog();

        target.General ??= new HatchingGeneralCatalog();
        defaults.General ??= new HatchingGeneralCatalog();

        target.General.UpdateRate ??= defaults.General.UpdateRate;
        target.General.ChosenPercentageDailyLuckBonus ??= defaults.General.ChosenPercentageDailyLuckBonus;
        target.General.HarvestType ??= defaults.General.HarvestType;
        target.General.HatchingMode ??= defaults.General.HatchingMode;
        target.General.BetterTrufflesCompat ??= defaults.General.BetterTrufflesCompat;
        target.General.EnableStuckInside ??= defaults.General.EnableStuckInside;
        target.General.EnableEggProbability ??= defaults.General.EnableEggProbability;
        target.General.EnableLargeEggProbability ??= defaults.General.EnableLargeEggProbability;
        target.General.EnableDebug ??= defaults.General.EnableDebug;
        target.General.EggProbability ??= defaults.General.EggProbability;
        target.General.LargeEggProbability ??= defaults.General.LargeEggProbability;
        target.General.FertilizedEggProbability ??= defaults.General.FertilizedEggProbability;
        target.General.RoosterToChickenRatio ??= defaults.General.RoosterToChickenRatio;
        target.General.BoostByRooster ??= defaults.General.BoostByRooster;

        target.Roosters ??= new Dictionary<string, HatchingRoosterCatalog>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, def) in defaults.Roosters ?? new Dictionary<string, HatchingRoosterCatalog>())
        {
            if (string.IsNullOrWhiteSpace(key) || def is null)
                continue;

            if (!target.Roosters.TryGetValue(key, out var existing) || existing is null)
            {
                target.Roosters[key] = def;
                continue;
            }

            existing.BuildingRequired ??= def.BuildingRequired;
            existing.Buyable ??= def.Buyable;
            existing.Price ??= def.Price;
            existing.EnableRoosterHatching ??= def.EnableRoosterHatching;
            existing.EditAllFemales ??= def.EditAllFemales;
            existing.MatureDays ??= def.MatureDays;
            existing.ProduceDays ??= def.ProduceDays;
            existing.ProduceOption ??= def.ProduceOption;
            existing.DeluxeProduceOption ??= def.DeluxeProduceOption;
            existing.BypassBlueConditions ??= def.BypassBlueConditions;
        }
    }
}
