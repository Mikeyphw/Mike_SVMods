using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RoosterAddon.Framework.Config;
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
        IMonitor monitor)
    {
        if (config is null) throw new ArgumentNullException(nameof(config));
        if (catalogs is null) throw new ArgumentNullException(nameof(catalogs));
        if (monitor is null) throw new ArgumentNullException(nameof(monitor));

        var changed = false;

        // Always seed a brand-new config from catalogs so validation passes and GMCM has sane defaults.
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

            case CatalogApplyMode.RepairInvalid:
                changed |= FixInvalidProduceTables(config, catalogs.Produce, monitor);
                return changed;

            case CatalogApplyMode.MergeMissing:
                changed |= MergeMissingDefaults(config, catalogs, monitor);
                changed |= FixInvalidProduceTables(config, catalogs.Produce, monitor);
                return changed;

            case CatalogApplyMode.Authoritative:
                changed |= ApplyProduceDefaults(config, catalogs.Produce);
                changed |= ApplyHatchingDefaults(config, catalogs.Hatching);
                return changed;

            default:
                changed |= FixInvalidProduceTables(config, catalogs.Produce, monitor);
                return changed;
        }
    }

    public CatalogBundle CreateFromConfig(RoosterAddonConfig config, CatalogBundle? baseCatalogs = null)
    {
        if (config is null) throw new ArgumentNullException(nameof(config));

        var defaults = CatalogDefaults.CreateDefaults();
        var catalogs = baseCatalogs ?? defaults;

        var produce = new ProduceCatalog
        {
            SchemaVersion = catalogs.Produce.SchemaVersion,
            Roosters = new Dictionary<string, ProduceCatalogEntry>(StringComparer.OrdinalIgnoreCase)
        };

        foreach (var (rt, settings) in config.Roosters)
        {
            produce.Roosters[rt.ToString()] = new ProduceCatalogEntry
            {
                BlueEggChoice = settings.BlueEggChoice,
                Produce = settings.ProduceEntries.Select(ToDto).ToList(),
                Deluxe = settings.DeluxeProduceEntries.Select(ToDto).ToList()
            };
        }

        var hatching = new HatchingCatalog
        {
            SchemaVersion = catalogs.Hatching.SchemaVersion,
            General = new HatchingGeneralCatalog
            {
                HarvestType = config.General.HarvestType.ToString(),
                HatchingMode = config.General.HatchingMode.ToString()
            },
            Roosters = new Dictionary<string, HatchingRoosterCatalog>(StringComparer.OrdinalIgnoreCase)
        };

        foreach (var (rt, settings) in config.Roosters)
        {
            hatching.Roosters[rt.ToString()] = new HatchingRoosterCatalog
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

        return new CatalogBundle
        {
            Roosters = catalogs.Roosters,
            Produce = produce,
            Hatching = hatching
        };
    }

    public void WriteAll(IModHelper helper, IMonitor monitor, CatalogBundle catalogs)
    {
        if (helper is null) throw new ArgumentNullException(nameof(helper));
        if (monitor is null) throw new ArgumentNullException(nameof(monitor));
        if (catalogs is null) throw new ArgumentNullException(nameof(catalogs));

        EnsureFolderExists(Path.Combine(helper.DirectoryPath, "data", CatalogRoot));

        helper.Data.WriteJsonFile(RoostersFile, catalogs.Roosters);
        helper.Data.WriteJsonFile(ProduceFile, catalogs.Produce);
        helper.Data.WriteJsonFile(HatchingFile, catalogs.Hatching);

        monitor.Log("Catalogs exported to data/catalogs.", LogLevel.Info);
    }

    private static bool MergeMissingDefaults(RoosterAddonConfig config, CatalogBundle catalogs, IMonitor monitor)
    {
        var changed = false;
        var defaults = RoosterAddonConfig.CreateDefault();

        if (config.General.HarvestType == defaults.General.HarvestType
            && Enum.TryParse<HarvestType>(catalogs.Hatching.General.HarvestType ?? string.Empty, true, out var harvest))
        {
            config.General.HarvestType = harvest;
            changed = true;
        }

        if (config.General.HatchingMode == defaults.General.HatchingMode
            && Enum.TryParse<HatchingMode>(catalogs.Hatching.General.HatchingMode ?? string.Empty, true, out var mode))
        {
            config.General.HatchingMode = mode;
            changed = true;
        }

        foreach (var (rt, settings) in config.Roosters)
        {
            if (!catalogs.Hatching.Roosters.TryGetValue(rt.ToString(), out var h))
                continue;

            var def = defaults.Roosters[rt];

            if (settings.BuildingRequired == def.BuildingRequired
                && Enum.TryParse<RequiredBuilding>(h.BuildingRequired ?? string.Empty, true, out var b))
            {
                settings.BuildingRequired = b;
                changed = true;
            }

            if (settings.Buyable == def.Buyable && h.Buyable.HasValue) { settings.Buyable = h.Buyable.Value; changed = true; }
            if (settings.Price == def.Price && h.Price.HasValue) { settings.Price = h.Price.Value; changed = true; }
            if (settings.EnableRoosterHatching == def.EnableRoosterHatching && h.EnableRoosterHatching.HasValue) { settings.EnableRoosterHatching = h.EnableRoosterHatching.Value; changed = true; }
            if (settings.MatureDays == def.MatureDays && h.MatureDays.HasValue) { settings.MatureDays = h.MatureDays.Value; changed = true; }
            if (settings.ProduceDays == def.ProduceDays && h.ProduceDays.HasValue) { settings.ProduceDays = h.ProduceDays.Value; changed = true; }
            if (settings.BypassBlueConditions == def.BypassBlueConditions && h.BypassBlueConditions.HasValue) { settings.BypassBlueConditions = h.BypassBlueConditions.Value; changed = true; }

            if (settings.ProduceOption == def.ProduceOption
                && Enum.TryParse<ProduceMode>(h.ProduceOption ?? string.Empty, true, out var p))
            {
                settings.ProduceOption = p;
                changed = true;
            }

            if (settings.DeluxeProduceOption == def.DeluxeProduceOption
                && Enum.TryParse<DeluxeProduceMode>(h.DeluxeProduceOption ?? string.Empty, true, out var d))
            {
                settings.DeluxeProduceOption = d;
                changed = true;
            }

            if (catalogs.Produce.Roosters.TryGetValue(rt.ToString(), out var prod))
            {
                if (string.Equals(settings.BlueEggChoice, def.BlueEggChoice, StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrWhiteSpace(prod.BlueEggChoice))
                {
                    settings.BlueEggChoice = prod.BlueEggChoice!;
                    changed = true;
                }

                if (SequenceEquals(settings.ProduceEntries, def.ProduceEntries) && prod.Produce.Count > 0)
                {
                    settings.ProduceEntries = prod.Produce.Select(ToConfigEntry).ToList();
                    changed = true;
                }

                if (SequenceEquals(settings.DeluxeProduceEntries, def.DeluxeProduceEntries) && prod.Deluxe.Count > 0)
                {
                    settings.DeluxeProduceEntries = prod.Deluxe.Select(ToConfigEntry).ToList();
                    changed = true;
                }
            }
        }

        if (changed)
            monitor.Log("CatalogApplyMode=MergeMissing: filled defaults from catalogs where config matched factory defaults.", LogLevel.Info);

        return changed;
    }

    private static bool SequenceEquals(IReadOnlyList<ProduceEntry> a, IReadOnlyList<ProduceEntry> b)
    {
        if (a.Count != b.Count)
            return false;

        for (var i = 0; i < a.Count; i++)
        {
            if (!string.Equals(a[i].Choice, b[i].Choice, StringComparison.OrdinalIgnoreCase))
                return false;
            if (!string.Equals(a[i].CustomItemName, b[i].CustomItemName, StringComparison.OrdinalIgnoreCase))
                return false;
            if (a[i].Probability != b[i].Probability)
                return false;
        }

        return true;
    }

    private static ProduceEntryDto ToDto(ProduceEntry entry)
        => new()
        {
            Choice = entry.Choice ?? string.Empty,
            CustomItemName = entry.CustomItemName ?? string.Empty,
            Probability = entry.Probability
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
