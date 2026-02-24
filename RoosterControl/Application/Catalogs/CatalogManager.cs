using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RoosterControl.Enums;
using RoosterControl.Framework.Config;
using StardewModdingAPI;

namespace RoosterControl.Application.Catalogs;

internal sealed class CatalogManager
{
    private const string CatalogRoot = "catalogs";
    private const string RoostersFile = "catalogs/roosters.json";
    private const string ProduceFile = "catalogs/produce.json";
    private const string HatchingFile = "catalogs/hatching.json";

    public CatalogBundle LoadOrCreateAll(IModHelper helper, IMonitor monitor)
    {
        if (helper is null) throw new ArgumentNullException(nameof(helper));
        if (monitor is null) throw new ArgumentNullException(nameof(monitor));

        var defaults = CatalogDefaults.CreateDefaults();
        EnsureFolderExists(Path.Combine(helper.DirectoryPath, "data", CatalogRoot));

        var bundle = new CatalogBundle
        {
            Roosters = ReadOrCreate(helper, RoostersFile, defaults.Roosters),
            Produce = ReadOrCreate(helper, ProduceFile, defaults.Produce),
            Hatching = ReadOrCreate(helper, HatchingFile, defaults.Hatching),
        };

        CatalogMigrator.MergeMissing(bundle, defaults);
        CatalogSanitizer.Sanitize(bundle);

        // Normalize on disk so users have stable formatting and clamped probability values.
        helper.Data.WriteJsonFile(RoostersFile, bundle.Roosters);
        helper.Data.WriteJsonFile(ProduceFile, bundle.Produce);
        helper.Data.WriteJsonFile(HatchingFile, bundle.Hatching);

        var issues = CatalogValidator.Validate(bundle);
        if (issues.Count > 0)
        {
            monitor.Log("Catalog validation errors (fatal):", LogLevel.Error);
            foreach (var issue in issues)
                monitor.Log($"  - {issue}", LogLevel.Error);

            throw new InvalidOperationException("RoosterControl catalogs are invalid. See SMAPI log for details.");
        }

        return bundle;
    }

    public bool ApplyToConfig(
        RoosterControlConfig config,
        CatalogBundle catalogs,
        bool isNewConfig,
        CatalogApplyMode mode,
        IMonitor monitor
    )
    {

if (config is null) throw new ArgumentNullException(nameof(config));
if (catalogs is null) throw new ArgumentNullException(nameof(catalogs));
if (monitor is null) throw new ArgumentNullException(nameof(monitor));

return CatalogApplicator.ApplyToConfig(
    config,
    catalogs,
    isNewConfig,
    mode,
    (level, message) =>
    {
        var smapiLevel = level switch
        {
            CatalogApplicator.CatalogLogLevel.Warn => LogLevel.Warn,
            CatalogApplicator.CatalogLogLevel.Error => LogLevel.Error,
            _ => LogLevel.Info
        };

        monitor.Log(message, smapiLevel);
    }
);
}

#if false
    private static bool EnsureRequiredRoosters(RoosterControlConfig config, CatalogBundle catalogs, IMonitor monitor)
    {
        var changed = false;

        var required = catalogs?.Roosters?.Roosters ?? new Dictionary<string, RoosterCatalogEntry>();
        foreach (var key in required.Keys)
        {
            if (!Enum.TryParse<RoosterType>(key ?? string.Empty, ignoreCase: true, out var roosterType))
                continue;

            if (config.Roosters.ContainsKey(roosterType))
                continue;

            config.Roosters[roosterType] = new RoosterSettings();
            changed = true;
        }

        if (changed)
            monitor.Log("Catalogs: added missing RoosterSettings entries for required rooster types.", LogLevel.Warn);

        return changed;
    }

    private static bool FixInvalidGeneralSettings(RoosterControlConfig config, HatchingGeneralCatalog general, IMonitor monitor)
    {
        if (config.General is null)
            config.General = new GeneralSettings();

        var changed = false;

        changed |= ClampInt(ref config.General.ChosenPercentageDailyLuckBonus, 0, 100, nameof(config.General.ChosenPercentageDailyLuckBonus), monitor);
        changed |= ClampInt(ref config.General.EggProbability, 0, 100, nameof(config.General.EggProbability), monitor);
        changed |= ClampInt(ref config.General.LargeEggProbability, 0, 100, nameof(config.General.LargeEggProbability), monitor);
        changed |= ClampInt(ref config.General.FertilizedEggProbability, 0, 100, nameof(config.General.FertilizedEggProbability), monitor);
        changed |= ClampInt(ref config.General.RoosterToChickenRatio, 0, 100, nameof(config.General.RoosterToChickenRatio), monitor);

        changed |= ClampFloat(ref config.General.BoostByRooster, 0f, 1000f, nameof(config.General.BoostByRooster), monitor);

        return changed;
    }

    private static bool FixInvalidRoosterSettings(RoosterControlConfig config, HatchingCatalog hatching, IMonitor monitor)
    {
        var changed = false;

        foreach (var (rt, settings) in config.Roosters)
        {
            if (settings is null)
                continue;

            if (settings.Price < 0) { settings.Price = 0; changed = true; monitor.Log($"Config: repaired negative Price for {rt}.", LogLevel.Warn); }
            if (settings.MatureDays < 0) { settings.MatureDays = 0; changed = true; monitor.Log($"Config: repaired negative MatureDays for {rt}.", LogLevel.Warn); }
            if (settings.ProduceDays < 0) { settings.ProduceDays = 0; changed = true; monitor.Log($"Config: repaired negative ProduceDays for {rt}.", LogLevel.Warn); }
        }

        return changed;
    }

    private static bool ClampInt(ref int value, int min, int max, string name, IMonitor monitor)
    {
        var clamped = Math.Clamp(value, min, max);
        if (clamped == value)
            return false;

        value = clamped;
        monitor.Log($"Config: clamped {name} to {value}.", LogLevel.Warn);
        return true;
    }

    private static bool ClampFloat(ref float value, float min, float max, string name, IMonitor monitor)
    {
        var clamped = Math.Clamp(value, min, max);
        if (Math.Abs(clamped - value) < 0.0001f)
            return false;

        value = clamped;
        monitor.Log($"Config: clamped {name} to {value}.", LogLevel.Warn);
        return true;
    }

    private static bool ApplyProduceDefaults(RoosterControlConfig config, ProduceCatalog produce)
    {
        var changed = false;

        foreach (var (rt, settings) in config.Roosters)
        {
            if (settings is null)
                continue;

            if (!produce.Roosters.TryGetValue(rt.ToString(), out var entry) || entry is null)
                continue;

            if (!string.IsNullOrWhiteSpace(entry.BlueEggChoice) && !string.Equals(settings.BlueEggChoice, entry.BlueEggChoice, StringComparison.Ordinal))
            {
                settings.BlueEggChoice = entry.BlueEggChoice!;
                changed = true;
            }

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
        }

        return changed;
    }

    private static bool ApplyHatchingDefaults(RoosterControlConfig config, HatchingCatalog hatching)
    {
        var changed = false;

        if (config.General is null)
            config.General = new GeneralSettings();

        var g = hatching?.General;
        if (g is not null)
        {
            if (Enum.TryParse<UpdateRateMode>(g.UpdateRate ?? string.Empty, ignoreCase: true, out var updateRate))
            {
                config.General.UpdateRate = updateRate;
                changed = true;
            }

            if (g.ChosenPercentageDailyLuckBonus is { } luck)
            {
                config.General.ChosenPercentageDailyLuckBonus = Math.Clamp(luck, 0, 100);
                changed = true;
            }

            if (Enum.TryParse<HarvestType>(g.HarvestType ?? string.Empty, ignoreCase: true, out var harvest))
            {
                config.General.HarvestType = harvest;
                changed = true;
            }

            if (Enum.TryParse<HatchingMode>(g.HatchingMode ?? string.Empty, ignoreCase: true, out var hatchMode))
            {
                config.General.HatchingMode = hatchMode;
                changed = true;
            }

            if (g.BetterTrufflesCompat.HasValue) { config.General.BetterTrufflesCompat = g.BetterTrufflesCompat.Value; changed = true; }
            if (g.EnableStuckInside.HasValue) { config.General.EnableStuckInside = g.EnableStuckInside.Value; changed = true; }
            if (g.EnableEggProbability.HasValue) { config.General.EnableEggProbability = g.EnableEggProbability.Value; changed = true; }
            if (g.EnableLargeEggProbability.HasValue) { config.General.EnableLargeEggProbability = g.EnableLargeEggProbability.Value; changed = true; }
            if (g.EnableDebug.HasValue) { config.General.EnableDebug = g.EnableDebug.Value; changed = true; }

            if (g.EggProbability is { } eggP) { config.General.EggProbability = Math.Clamp(eggP, 0, 100); changed = true; }
            if (g.LargeEggProbability is { } largeP) { config.General.LargeEggProbability = Math.Clamp(largeP, 0, 100); changed = true; }
            if (g.FertilizedEggProbability is { } fertP) { config.General.FertilizedEggProbability = Math.Clamp(fertP, 0, 100); changed = true; }
            if (g.RoosterToChickenRatio is { } ratio) { config.General.RoosterToChickenRatio = Math.Clamp(ratio, 0, 100); changed = true; }
            if (g.BoostByRooster is { } boost) { config.General.BoostByRooster = Math.Clamp(boost, 0f, 1000f); changed = true; }
        }

        foreach (var (rt, settings) in config.Roosters)
        {
            if (settings is null)
                continue;

            if (hatching?.Roosters is null || !hatching.Roosters.TryGetValue(rt.ToString(), out var h) || h is null)
                continue;

            if (Enum.TryParse<RequiredBuilding>(h.BuildingRequired ?? string.Empty, ignoreCase: true, out var b))
            {
                settings.BuildingRequired = b;
                changed = true;
            }

            if (h.Buyable.HasValue) { settings.Buyable = h.Buyable.Value; changed = true; }
            if (h.Price.HasValue) { settings.Price = Math.Max(0, h.Price.Value); changed = true; }
            if (h.EnableRoosterHatching.HasValue) { settings.EnableRoosterHatching = h.EnableRoosterHatching.Value; changed = true; }
            if (h.EditAllFemales.HasValue) { settings.EditAllFemales = h.EditAllFemales.Value; changed = true; }
            if (h.MatureDays.HasValue) { settings.MatureDays = Math.Max(0, h.MatureDays.Value); changed = true; }
            if (h.ProduceDays.HasValue) { settings.ProduceDays = Math.Max(0, h.ProduceDays.Value); changed = true; }
            if (h.BypassBlueConditions.HasValue) { settings.BypassBlueConditions = h.BypassBlueConditions.Value; changed = true; }

            if (Enum.TryParse<ProduceMode>(h.ProduceOption ?? string.Empty, ignoreCase: true, out var p))
            {
                settings.ProduceOption = p;
                changed = true;
            }

            if (Enum.TryParse<DeluxeProduceMode>(h.DeluxeProduceOption ?? string.Empty, ignoreCase: true, out var d))
            {
                settings.DeluxeProduceOption = d;
                changed = true;
            }
        }

        return changed;
    }

    private static bool FixInvalidProduceTables(RoosterControlConfig config, ProduceCatalog produce, IMonitor monitor)
    {
        var changed = false;

        foreach (var (rt, settings) in config.Roosters)
        {
            if (!produce.Roosters.TryGetValue(rt.ToString(), out var entry))
                continue;

            if (settings.ProduceOption == ProduceMode.CustomizeBelow && !HasAnyUsable(settings.ProduceEntries))
            {
                settings.ProduceEntries = entry.Produce.Select(ToConfigEntry).ToList();
                changed = true;
                monitor.Log($"Catalogs: repaired invalid ProduceEntries for {rt}.", LogLevel.Warn);
            }

            if (settings.DeluxeProduceOption == DeluxeProduceMode.CustomizeBelow && !HasAnyUsable(settings.DeluxeProduceEntries))
            {
                settings.DeluxeProduceEntries = entry.Deluxe.Select(ToConfigEntry).ToList();
                changed = true;
                monitor.Log($"Catalogs: repaired invalid DeluxeProduceEntries for {rt}.", LogLevel.Warn);
            }

            if (rt.ToString().Equals("Blue", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(entry.BlueEggChoice))
                if (string.IsNullOrWhiteSpace(settings.BlueEggChoice))
                {
                    settings.BlueEggChoice = entry.BlueEggChoice;
                    changed = true;
                    monitor.Log($"Catalogs: repaired empty BlueEggChoice for {rt}.", LogLevel.Warn);
                }
        }

        return changed;
    }

    private static bool MergeMissing(RoosterControlConfig config, CatalogBundle catalogs, IMonitor monitor)
    {
        var changed = false;

        foreach (var (rt, settings) in config.Roosters)
        {
            if (!catalogs.Produce.Roosters.TryGetValue(rt.ToString(), out var produce))
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
            monitor.Log("Catalogs merged missing entries into config.", LogLevel.Info);

        return changed;
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
#endif

    private static T ReadOrCreate<T>(IModHelper helper, string relativePath, T defaults)
        where T : class, new()
    {
        var loaded = helper.Data.ReadJsonFile<T>(relativePath);
        if (loaded is null)
        {
            helper.Data.WriteJsonFile(relativePath, defaults);
            return defaults;
        }

        return loaded;
    }

    private static void EnsureFolderExists(string path)
    {
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
    }
}
