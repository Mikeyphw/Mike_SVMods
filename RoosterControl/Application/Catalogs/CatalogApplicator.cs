using System;
using System.Collections.Generic;
using System.Linq;
using RoosterControl.Enums;
using RoosterControl.Framework.Config;

namespace RoosterControl.Application.Catalogs;

internal static class CatalogApplicator
{
    internal enum CatalogLogLevel { Info, Warn, Error }

    internal static bool ApplyToConfig(
        RoosterControlConfig config,
        CatalogBundle catalogs,
        bool isNewConfig,
        CatalogApplyMode mode,
        Action<CatalogLogLevel, string>? log = null
    )
    {
        if (config is null) throw new ArgumentNullException(nameof(config));
        if (catalogs is null) throw new ArgumentNullException(nameof(catalogs));

        var changed = EnsureRequiredRoosters(config, catalogs, log);

        if (isNewConfig || mode == CatalogApplyMode.Authoritative)
        {
            changed |= ApplyHatchingDefaults(config, catalogs.Hatching);
            changed |= ApplyProduceDefaults(config, catalogs.Produce);
            return changed;
        }

        if (mode == CatalogApplyMode.MergeMissing)
            changed |= MergeMissing(config, catalogs, log);

        changed |= FixInvalidGeneralSettings(config, log);
        changed |= FixInvalidRoosterSettings(config, log);
        changed |= FixInvalidProduceTables(config, catalogs.Produce, log);
        return changed;
    }

    private static bool EnsureRequiredRoosters(RoosterControlConfig config, CatalogBundle catalogs, Action<CatalogLogLevel, string>? log)
    {
        var changed = false;
        foreach (var key in catalogs.Roosters.Roosters.Keys)
        {
            if (!Enum.TryParse<RoosterType>(key ?? string.Empty, true, out var roosterType))
                continue;

            if (config.Roosters.ContainsKey(roosterType))
                continue;

            config.Roosters[roosterType] = new RoosterSettings();
            changed = true;
        }

        if (changed)
            log?.Invoke(CatalogLogLevel.Warn, "Catalogs: added missing RoosterSettings entries.");

        return changed;
    }

    private static bool FixInvalidGeneralSettings(RoosterControlConfig config, Action<CatalogLogLevel, string>? log)
    {
        config.General ??= new GeneralSettings();
        var changed = false;

        changed |= ClampAndAssign(config.General.ChosenPercentageDailyLuckBonus, 0, 100, v => config.General.ChosenPercentageDailyLuckBonus = v);
        changed |= ClampAndAssign(config.General.EggProbability, 0, 100, v => config.General.EggProbability = v);
        changed |= ClampAndAssign(config.General.LargeEggProbability, 0, 100, v => config.General.LargeEggProbability = v);
        changed |= ClampAndAssign(config.General.FertilizedEggProbability, 0, 100, v => config.General.FertilizedEggProbability = v);
        changed |= ClampAndAssign(config.General.RoosterToChickenRatio, 0, 100, v => config.General.RoosterToChickenRatio = v);
        changed |= ClampAndAssign(config.General.BoostByRooster, 0f, 1000f, v => config.General.BoostByRooster = v);

        if (changed)
            log?.Invoke(CatalogLogLevel.Warn, "Catalogs: clamped invalid general config values.");

        return changed;
    }

    private static bool FixInvalidRoosterSettings(RoosterControlConfig config, Action<CatalogLogLevel, string>? log)
    {
        var changed = false;
        foreach (var (_, settings) in config.Roosters)
        {
            if (settings is null)
                continue;

            if (settings.Price < 0) { settings.Price = 0; changed = true; }
            if (settings.MatureDays < 0) { settings.MatureDays = 0; changed = true; }
            if (settings.ProduceDays < 0) { settings.ProduceDays = 0; changed = true; }
        }

        if (changed)
            log?.Invoke(CatalogLogLevel.Warn, "Catalogs: repaired invalid rooster scalar settings.");

        return changed;
    }

    private static bool ApplyProduceDefaults(RoosterControlConfig config, ProduceCatalog produce)
    {
        var changed = false;

        foreach (var (rt, settings) in config.Roosters)
        {
            if (!produce.Roosters.TryGetValue(rt.ToString(), out var entry) || entry is null || settings is null)
                continue;

            if (entry.Produce is { Count: > 0 })
            {
                settings.ProduceEntries = entry.Produce.Select(ToConfigEntry).ToList();
                changed = true;
            }

            if (entry.Deluxe is { Count: > 0 })
            {
                settings.DeluxeProduceEntries = entry.Deluxe.Select(ToConfigEntry).ToList();
                changed = true;
            }

            if (!string.IsNullOrWhiteSpace(entry.BlueEggChoice))
            {
                settings.BlueEggChoice = entry.BlueEggChoice!;
                changed = true;
            }
        }

        return changed;
    }

    private static bool ApplyHatchingDefaults(RoosterControlConfig config, HatchingCatalog hatching)
    {
        var changed = false;
        config.General ??= new GeneralSettings();

        var g = hatching?.General;
        if (g is not null)
        {
            if (Enum.TryParse<UpdateRateMode>(g.UpdateRate ?? string.Empty, true, out var updateRate))
            {
                config.General.UpdateRate = updateRate;
                changed = true;
            }

            if (Enum.TryParse<HarvestType>(g.HarvestType ?? string.Empty, true, out var harvestType))
            {
                config.General.HarvestType = harvestType;
                changed = true;
            }

            if (Enum.TryParse<HatchingMode>(g.HatchingMode ?? string.Empty, true, out var hatchingMode))
            {
                config.General.HatchingMode = hatchingMode;
                changed = true;
            }
        }

        return changed;
    }

    private static bool FixInvalidProduceTables(RoosterControlConfig config, ProduceCatalog produce, Action<CatalogLogLevel, string>? log)
    {
        var changed = false;

        foreach (var (rt, settings) in config.Roosters)
        {
            if (settings is null || !produce.Roosters.TryGetValue(rt.ToString(), out var entry))
                continue;

            settings.ProduceEntries ??= new List<ProduceEntry>();
            settings.DeluxeProduceEntries ??= new List<ProduceEntry>();

            if (settings.ProduceOption == ProduceMode.CustomizeBelow && !HasAnyUsable(settings.ProduceEntries))
            {
                settings.ProduceEntries = entry.Produce.Select(ToConfigEntry).ToList();
                changed = true;
            }

            if (settings.DeluxeProduceOption == DeluxeProduceMode.CustomizeBelow && !HasAnyUsable(settings.DeluxeProduceEntries))
            {
                settings.DeluxeProduceEntries = entry.Deluxe.Select(ToConfigEntry).ToList();
                changed = true;
            }
        }

        if (changed)
            log?.Invoke(CatalogLogLevel.Warn, "Catalogs: repaired invalid produce table entries.");

        return changed;
    }

    private static bool MergeMissing(RoosterControlConfig config, CatalogBundle catalogs, Action<CatalogLogLevel, string>? log)
    {
        var changed = false;

        foreach (var (rt, settings) in config.Roosters)
        {
            if (settings is null || !catalogs.Produce.Roosters.TryGetValue(rt.ToString(), out var produce))
                continue;

            if (settings.ProduceEntries is null || settings.ProduceEntries.Count == 0)
            {
                settings.ProduceEntries = produce.Produce.Select(ToConfigEntry).ToList();
                changed = true;
            }

            if (settings.DeluxeProduceEntries is null || settings.DeluxeProduceEntries.Count == 0)
            {
                settings.DeluxeProduceEntries = produce.Deluxe.Select(ToConfigEntry).ToList();
                changed = true;
            }
        }

        if (changed)
            log?.Invoke(CatalogLogLevel.Info, "Catalogs: merged missing entries into config.");

        return changed;
    }

    private static bool ClampAndAssign(int value, int min, int max, Action<int> assign)
    {
        var clamped = Math.Clamp(value, min, max);
        if (clamped == value)
            return false;

        assign(clamped);
        return true;
    }

    private static bool ClampAndAssign(float value, float min, float max, Action<float> assign)
    {
        var clamped = Math.Clamp(value, min, max);
        if (Math.Abs(clamped - value) < 0.0001f)
            return false;

        assign(clamped);
        return true;
    }

    private static bool HasAnyUsable(List<ProduceEntry> entries)
    {
        entries ??= new List<ProduceEntry>();
        return entries.Any(e =>
        {
            if (e is null)
                return false;

            var choice = (e.Choice ?? string.Empty).Trim();
            if (choice.Equals("Custom", StringComparison.OrdinalIgnoreCase))
                choice = (e.CustomItemName ?? string.Empty).Trim();

            return e.Probability > 0 && !string.IsNullOrWhiteSpace(choice);
        });
    }

    private static ProduceEntry ToConfigEntry(ProduceEntryDto dto)
        => new()
        {
            Choice = dto.Choice ?? string.Empty,
            CustomItemName = dto.CustomItemName ?? string.Empty,
            Probability = Math.Clamp(dto.Probability, 0, 100)
        };
}
