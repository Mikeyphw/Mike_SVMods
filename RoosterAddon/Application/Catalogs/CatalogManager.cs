using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RoosterAddon.Framework.Config;
using RoosterAddon.Enums;
using StardewModdingAPI;

namespace RoosterAddon.Application.Catalogs;

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

        var roosters = ReadOrCreate(helper, monitor, RoostersFile, defaults.Roosters);
        var produce = ReadOrCreate(helper, monitor, ProduceFile, defaults.Produce);
        var hatching = ReadOrCreate(helper, monitor, HatchingFile, defaults.Hatching);

        return new CatalogBundle { Roosters = roosters, Produce = produce, Hatching = hatching };
    }

    public bool ApplyToConfig(
    RoosterAddonConfig config,
    CatalogBundle catalogs,
    bool isNewConfig,
    IMonitor monitor
)
{
    if (config is null) throw new ArgumentNullException(nameof(config));
    if (catalogs is null) throw new ArgumentNullException(nameof(catalogs));
    if (monitor is null) throw new ArgumentNullException(nameof(monitor));

    var changed = false;

    // New config: seed config from catalogs so validation passes and GMCM has something sane.
    if (isNewConfig)
    {
        changed |= ApplyProduceDefaults(config, catalogs.Produce);
        changed |= ApplyHatchingDefaults(config, catalogs.Hatching);
        if (changed)
            monitor.Log("Catalogs applied to newly created config.", LogLevel.Info);

        return changed;
    }

    switch (config.General.CatalogApplyMode)
    {
        case CatalogApplyMode.SeedOnly:
            return false;

        case CatalogApplyMode.Authoritative:
            changed |= ApplyProduceDefaults(config, catalogs.Produce);
            changed |= ApplyHatchingDefaults(config, catalogs.Hatching);
            if (changed)
                monitor.Log("Catalogs applied authoritatively to config.", LogLevel.Info);
            return changed;

        case CatalogApplyMode.MergeMissing:
            changed |= MergeMissingFromCatalogs(config, catalogs, monitor);
            return changed;

        case CatalogApplyMode.RepairInvalid:
        default:
            // Existing config: only fix clearly-invalid produce tables (avoid clobbering user edits).
            changed |= FixInvalidProduceTables(config, catalogs.Produce, monitor);
            return changed;
    }
}

public void ExportEffective(IModHelper helper, IMonitor monitor, RoosterAddonConfig config, CatalogBundle currentCatalogs)
{
    if (helper is null) throw new ArgumentNullException(nameof(helper));
    if (monitor is null) throw new ArgumentNullException(nameof(monitor));
    if (config is null) throw new ArgumentNullException(nameof(config));
    if (currentCatalogs is null) throw new ArgumentNullException(nameof(currentCatalogs));

    // Preserve the rooster catalog (it is mostly structural keys).
    helper.Data.WriteJsonFile(RoostersFile, currentCatalogs.Roosters);

    var produce = BuildProduceFromConfig(config, currentCatalogs.Produce.SchemaVersion);
    helper.Data.WriteJsonFile(ProduceFile, produce);

    var hatching = BuildHatchingFromConfig(config, currentCatalogs.Hatching.SchemaVersion);
    helper.Data.WriteJsonFile(HatchingFile, hatching);

    monitor.Log("Catalogs exported to data/catalogs/*.json.", LogLevel.Info);
}

private static bool MergeMissingFromCatalogs(RoosterAddonConfig config, CatalogBundle catalogs, IMonitor monitor)
{
    var changed = false;

    var defaultGeneral = new GeneralSettings();
    if (config.General.HarvestType == defaultGeneral.HarvestType)
    {
        if (Enum.TryParse<HarvestType>(catalogs.Hatching.General.HarvestType ?? string.Empty, ignoreCase: true, out var harvest))
        {
            config.General.HarvestType = harvest;
            changed = true;
        }
    }

    if (config.General.HatchingMode == defaultGeneral.HatchingMode)
    {
        if (Enum.TryParse<HatchingMode>(catalogs.Hatching.General.HatchingMode ?? string.Empty, ignoreCase: true, out var mode))
        {
            config.General.HatchingMode = mode;
            changed = true;
        }
    }

    foreach (var (rt, settings) in config.Roosters)
    {
        var defaults = new RoosterSettings();

        if (catalogs.Hatching.Roosters.TryGetValue(rt.ToString(), out var h))
        {
            if (settings.BuildingRequired == defaults.BuildingRequired
                && Enum.TryParse<RequiredBuilding>(h.BuildingRequired ?? string.Empty, ignoreCase: true, out var b))
            {
                settings.BuildingRequired = b;
                changed = true;
            }

            if (settings.Buyable == defaults.Buyable && h.Buyable.HasValue) { settings.Buyable = h.Buyable.Value; changed = true; }
            if (settings.Price == defaults.Price && h.Price.HasValue) { settings.Price = h.Price.Value; changed = true; }
            if (settings.EnableRoosterHatching == defaults.EnableRoosterHatching && h.EnableRoosterHatching.HasValue)
            {
                settings.EnableRoosterHatching = h.EnableRoosterHatching.Value;
                changed = true;
            }

            if (settings.MatureDays == defaults.MatureDays && h.MatureDays.HasValue) { settings.MatureDays = h.MatureDays.Value; changed = true; }
            if (settings.ProduceDays == defaults.ProduceDays && h.ProduceDays.HasValue) { settings.ProduceDays = h.ProduceDays.Value; changed = true; }

            if (settings.BypassBlueConditions == defaults.BypassBlueConditions && h.BypassBlueConditions.HasValue)
            {
                settings.BypassBlueConditions = h.BypassBlueConditions.Value;
                changed = true;
            }

            if (settings.ProduceOption == defaults.ProduceOption
                && Enum.TryParse<ProduceMode>(h.ProduceOption ?? string.Empty, ignoreCase: true, out var p))
            {
                settings.ProduceOption = p;
                changed = true;
            }

            if (settings.DeluxeProduceOption == defaults.DeluxeProduceOption
                && Enum.TryParse<DeluxeProduceMode>(h.DeluxeProduceOption ?? string.Empty, ignoreCase: true, out var d))
            {
                settings.DeluxeProduceOption = d;
                changed = true;
            }
        }

        if (catalogs.Produce.Roosters.TryGetValue(rt.ToString(), out var entry))
        {
            if (rt.ToString().Equals("Blue", StringComparison.OrdinalIgnoreCase)
                && (string.IsNullOrWhiteSpace(settings.BlueEggChoice) || settings.BlueEggChoice == defaults.BlueEggChoice)
                && !string.IsNullOrWhiteSpace(entry.BlueEggChoice))
            {
                settings.BlueEggChoice = entry.BlueEggChoice.Trim();
                changed = true;
            }

            if (settings.ProduceOption == ProduceMode.CustomizeBelow)
            {
                var shouldReplace = !HasAnyUsable(settings.ProduceEntries) || AreProduceListsEquivalent(settings.ProduceEntries, defaults.ProduceEntries);
                if (shouldReplace && entry.Produce.Count > 0)
                {
                    settings.ProduceEntries = entry.Produce.Select(ToConfigEntry).ToList();
                    changed = true;
                    monitor.Log($"Catalogs: merged ProduceEntries for {rt}.", LogLevel.Info);
                }
            }

            if (settings.DeluxeProduceOption == DeluxeProduceMode.CustomizeBelow)
            {
                var shouldReplace = !HasAnyUsable(settings.DeluxeProduceEntries) || AreProduceListsEquivalent(settings.DeluxeProduceEntries, defaults.DeluxeProduceEntries);
                if (shouldReplace && entry.Deluxe.Count > 0)
                {
                    settings.DeluxeProduceEntries = entry.Deluxe.Select(ToConfigEntry).ToList();
                    changed = true;
                    monitor.Log($"Catalogs: merged DeluxeProduceEntries for {rt}.", LogLevel.Info);
                }
            }
        }
    }

    // Always repair invalid tables (safe) even in merge mode.
    changed |= FixInvalidProduceTables(config, catalogs.Produce, monitor);

    if (changed)
        monitor.Log("Catalogs: merge-missing applied to config.", LogLevel.Info);

    return changed;
}

private static bool AreProduceListsEquivalent(IReadOnlyList<ProduceEntry> a, IReadOnlyList<ProduceEntry> b)
{
    if (a is null || b is null) return false;
    if (a.Count != b.Count) return false;

    for (var i = 0; i < a.Count; i++)
    {
        var ea = a[i];
        var eb = b[i];

        if (ea is null || eb is null) return false;

        if (!string.Equals((ea.Choice ?? string.Empty).Trim(), (eb.Choice ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase))
            return false;

        if (!string.Equals((ea.CustomItemName ?? string.Empty).Trim(), (eb.CustomItemName ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase))
            return false;

        if (ea.Probability != eb.Probability)
            return false;
    }

    return true;
}

private static ProduceCatalog BuildProduceFromConfig(RoosterAddonConfig config, int schemaVersion)
{
    var cat = new ProduceCatalog { SchemaVersion = schemaVersion };

    foreach (var (rt, settings) in config.Roosters)
    {
        var entry = new ProduceCatalogEntry
        {
            BlueEggChoice = rt.ToString().Equals("Blue", StringComparison.OrdinalIgnoreCase)
                ? (string.IsNullOrWhiteSpace(settings.BlueEggChoice) ? null : settings.BlueEggChoice)
                : null,
            Produce = (settings.ProduceEntries ?? new List<ProduceEntry>()).Select(ToDto).ToList(),
            Deluxe = (settings.DeluxeProduceEntries ?? new List<ProduceEntry>()).Select(ToDto).ToList(),
        };

        cat.Roosters[rt.ToString()] = entry;
    }

    return cat;
}

private static HatchingCatalog BuildHatchingFromConfig(RoosterAddonConfig config, int schemaVersion)
{
    var cat = new HatchingCatalog
    {
        SchemaVersion = schemaVersion,
        General = new HatchingGeneralCatalog
        {
            HarvestType = config.General.HarvestType.ToString(),
            HatchingMode = config.General.HatchingMode.ToString()
        }
    };

    foreach (var (rt, settings) in config.Roosters)
    {
        cat.Roosters[rt.ToString()] = new HatchingRoosterCatalog
        {
            BuildingRequired = settings.BuildingRequired.ToString(),
            Buyable = settings.Buyable,
            Price = settings.Price,
            EnableRoosterHatching = settings.EnableRoosterHatching,
            MatureDays = settings.MatureDays,
            ProduceDays = settings.ProduceDays,
            ProduceOption = settings.ProduceOption.ToString(),
            DeluxeProduceOption = settings.DeluxeProduceOption.ToString(),
            BypassBlueConditions = settings.BypassBlueConditions
        };
    }

    return cat;
}

private static ProduceEntryDto ToDto(ProduceEntry entry)
    => new()
    {
        Choice = entry?.Choice ?? string.Empty,
        CustomItemName = entry?.CustomItemName ?? string.Empty,
        Probability = Math.Clamp(entry?.Probability ?? 0, 0, 100)
    };

private static bool ApplyProduceDefaults(RoosterAddonConfig config, ProduceCatalog produce)
    {
        var changed = false;

        foreach (var (rt, settings) in config.Roosters)
        {
            if (!produce.Roosters.TryGetValue(rt.ToString(), out var entry))
                continue;

            settings.BlueEggChoice = entry.BlueEggChoice ?? settings.BlueEggChoice;

            if (entry.Produce.Count > 0)
            {
                settings.ProduceEntries = entry.Produce.Select(ToConfigEntry).ToList();
                changed = true;
            }

            if (entry.Deluxe.Count > 0)
            {
                settings.DeluxeProduceEntries = entry.Deluxe.Select(ToConfigEntry).ToList();
                changed = true;
            }
        }

        return changed;
    }

    private static bool ApplyHatchingDefaults(RoosterAddonConfig config, HatchingCatalog hatching)
    {
        var changed = false;

        if (Enum.TryParse<HarvestType>(hatching.General.HarvestType ?? string.Empty, ignoreCase: true, out var harvest))
        {
            config.General.HarvestType = harvest;
            changed = true;
        }

        if (Enum.TryParse<HatchingMode>(hatching.General.HatchingMode ?? string.Empty, ignoreCase: true, out var mode))
        {
            config.General.HatchingMode = mode;
            changed = true;
        }

        foreach (var (rt, settings) in config.Roosters)
        {
            if (!hatching.Roosters.TryGetValue(rt.ToString(), out var h))
                continue;

            if (Enum.TryParse<RequiredBuilding>(h.BuildingRequired ?? string.Empty, ignoreCase: true, out var b))
            {
                settings.BuildingRequired = b;
                changed = true;
            }

            if (h.Buyable.HasValue) { settings.Buyable = h.Buyable.Value; changed = true; }
            if (h.Price.HasValue) { settings.Price = h.Price.Value; changed = true; }
            if (h.EnableRoosterHatching.HasValue) { settings.EnableRoosterHatching = h.EnableRoosterHatching.Value; changed = true; }
            if (h.MatureDays.HasValue) { settings.MatureDays = h.MatureDays.Value; changed = true; }
            if (h.ProduceDays.HasValue) { settings.ProduceDays = h.ProduceDays.Value; changed = true; }
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

    private static bool FixInvalidProduceTables(RoosterAddonConfig config, ProduceCatalog produce, IMonitor monitor)
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
            {
                settings.BlueEggChoice ??= entry.BlueEggChoice;
            }
        }

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

    private static T ReadOrCreate<T>(IModHelper helper, IMonitor monitor, string relativePath, T defaults)
        where T : class, new()
    {
        var loaded = helper.Data.ReadJsonFile<T>(relativePath);
        if (loaded is null)
        {
            helper.Data.WriteJsonFile(relativePath, defaults);
            return defaults;
        }

        // Always write back once to normalize formatting + ensure file exists.
        helper.Data.WriteJsonFile(relativePath, loaded);
        return loaded;
    }

    private static void EnsureFolderExists(string path)
    {
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
    }
}
