using System;
using System.IO;
using System.Linq;
using RoosterControl.Constants;
using RoosterControl.Enums;
using StardewModdingAPI;
using Mikew.NestCore.Helpers.Logging;

using static Mikew.NestCore.Helpers.Logging.ScopedLogCatalog;

namespace RoosterControl.Framework.Config;

/// <summary>Loads and saves the typed RoosterControl configuration.</summary>
public static class ConfigManager
{
    private static ILog? _log;

    /// <summary>
    /// Load the typed config. If it's missing or invalid, create a fresh default and keep a backup of the old file.
    /// </summary>
    public static RoosterControlConfig LoadOrCreate(IModHelper helper, IMonitor monitor)
    {
        _log = new SmapiLog(monitor);

        var path = Path.Combine(helper.DirectoryPath, "config.json");

        if (!File.Exists(path))
        {
            var created = Normalize(RoosterControlConfig.CreateDefault());
            helper.WriteConfig(created);
            return created;
        }

        if (LegacyConfigAdapter.TryMigrateFromFile(path, monitor, out var migratedLegacy))
        {
            var backupPath = Path.Combine(
                helper.DirectoryPath,
                $"config.legacy.{DateTimeOffset.Now:yyyyMMddHHmmss}.json"
            );
            try
            {
                File.Copy(path, backupPath, overwrite: true);
            }
            catch
            {
                // best-effort backup
            }

            helper.WriteConfig(Normalize(migratedLegacy));
            LogScopedHelper.Log(_log!, ModRoosterControlCategory.SaveData, $"RoosterControl: wrote migrated config. Legacy backup: {backupPath}", LogLevel.Info);
            return Normalize(migratedLegacy);
        }

        try
        {
            var cfg = helper.ReadConfig<RoosterControlConfig>();
            cfg = Normalize(cfg);

            var originalVersion = cfg.SchemaVersion;

            if (cfg.SchemaVersion <= 0)
            {
                cfg.SchemaVersion = ConfigSchema.CurrentVersion;
                helper.WriteConfig(cfg);
                return cfg;
            }

            if (cfg.SchemaVersion != ConfigSchema.CurrentVersion)
                return HandleSchemaMismatch(helper, monitor, path, cfg, originalVersion);

            return cfg;
        }
        catch (Exception ex)
        {
            var backupPath = Path.Combine(
                helper.DirectoryPath,
                $"config.invalid.{DateTimeOffset.Now:yyyyMMddHHmmss}.json"
            );
            try
            {
                File.Copy(path, backupPath, overwrite: true);
            }
            catch
            {
                // best-effort backup
            }

            LogScopedHelper.Log(_log!, ModRoosterControlCategory.SaveData,
                $"RoosterControl: failed to read config.json, writing defaults. Backup: {backupPath}\n{ex}",
                LogLevel.Error
            );

            var created = Normalize(RoosterControlConfig.CreateDefault());
            helper.WriteConfig(created);
            return created;
        }
    }

    public static void Save(IModHelper helper, RoosterControlConfig cfg)
        => helper.WriteConfig(Normalize(cfg));

    private static RoosterControlConfig HandleSchemaMismatch(
        IModHelper helper,
        IMonitor monitor,
        string configPath,
        RoosterControlConfig cfg,
        int originalVersion
    )
    {
        var backupPath = Path.Combine(
            helper.DirectoryPath,
            $"config.schema.{originalVersion}.{DateTimeOffset.Now:yyyyMMddHHmmss}.json"
        );

        try
        {
            File.Copy(configPath, backupPath, overwrite: true);
        }
        catch
        {
            // best-effort backup
        }

        if (cfg.SchemaVersion < ConfigSchema.CurrentVersion)
        {
            LogScopedHelper.Log(_log!, ModRoosterControlCategory.SaveData,
                $"RoosterControl: upgrading config schema {cfg.SchemaVersion} -> {ConfigSchema.CurrentVersion}. Backup: {backupPath}",
                LogLevel.Info
            );

            cfg.SchemaVersion = ConfigSchema.CurrentVersion;
            cfg = Normalize(cfg);
            helper.WriteConfig(cfg);
            return cfg;
        }

        LogScopedHelper.Log(_log!, ModRoosterControlCategory.SaveData,
            $"RoosterControl: config schema version {cfg.SchemaVersion} is newer than supported {ConfigSchema.CurrentVersion}. Backup: {backupPath}",
            LogLevel.Warn
        );

        return cfg;
    }

    private static RoosterControlConfig Normalize(RoosterControlConfig cfg)
    {
        cfg ??= RoosterControlConfig.CreateDefault();

        cfg.General ??= new GeneralSettings();
        cfg.Roosters ??= new System.Collections.Generic.Dictionary<RoosterType, RoosterSettings>();
        cfg.FemalesOptions ??= new System.Collections.Generic.Dictionary<string, FemaleRoosterAssignment>();

        foreach (var (key, def) in RoosterControlConfig.CreateDefaultRoosters())
            cfg.Roosters.TryAdd(key, def);

        foreach (var (_, settings) in cfg.Roosters)
        {
            if (settings is null)
                continue;

            settings.ProduceEntries = NormalizeProduceEntries(settings.ProduceEntries);
            settings.DeluxeProduceEntries = NormalizeProduceEntries(settings.DeluxeProduceEntries);

            settings.BlueEggChoice = (settings.BlueEggChoice ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(settings.BlueEggChoice))
                settings.BlueEggChoice = ItemIds.CustomItems.MearaBlueEgg;
        }

        return cfg;
    }

    private static List<ProduceEntry> NormalizeProduceEntries(List<ProduceEntry>? entries)
    {
        entries ??= ProduceEntry.CreateDefaultFour();

        while (entries.Count < 4)
            entries.Add(new ProduceEntry());

        while (entries.Count > 4)
            entries.RemoveAt(entries.Count - 1);

        for (var i = 0; i < entries.Count; i++)
        {
            entries[i] ??= new ProduceEntry();

            entries[i].Choice = (entries[i].Choice ?? string.Empty).Trim();
            entries[i].CustomItemName = (entries[i].CustomItemName ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(entries[i].Choice))
                entries[i].Choice = ItemIds.CustomItems.Custom;

            if (string.IsNullOrWhiteSpace(entries[i].CustomItemName))
                entries[i].CustomItemName = ItemIds.CustomItems.Custom;

            entries[i].Probability = Math.Clamp(entries[i].Probability, 0, 100);
        }

        NormalizeWeightsTo100(entries);
        return entries;
    }

    private static void NormalizeWeightsTo100(List<ProduceEntry> entries)
    {
        var sum = entries.Sum(e => Math.Max(0, Math.Clamp(e?.Probability ?? 0, 0, 100)));
        if (sum <= 0 || sum == 100)
            return;

        var exact = entries.Select(e => (e: e, exact: (double)Math.Max(0, Math.Clamp(e?.Probability ?? 0, 0, 100)) * 100.0 / sum)).ToList();
        var floors = exact.Select(x => (x.e, floor: (int)Math.Floor(x.exact), rem: x.exact - Math.Floor(x.exact))).ToList();

        var total = floors.Sum(x => x.floor);
        var leftover = 100 - total;

        foreach (var x in floors)
            x.e.Probability = Math.Clamp(x.floor, 0, 100);

        if (leftover <= 0)
            return;

        foreach (var x in floors.OrderByDescending(x => x.rem))
        {
            if (leftover <= 0)
                break;

            x.e.Probability = Math.Clamp(x.e.Probability + 1, 0, 100);
            leftover--;
        }
    }


}


