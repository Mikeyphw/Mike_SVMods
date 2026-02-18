using System;
using System.IO;
using RoosterAddon.Enums;
using StardewModdingAPI;

namespace RoosterAddon.Framework.Config;

/// <summary>Loads and saves the typed RoosterAddon configuration.</summary>
public static class ConfigManager
{
    /// <summary>
    /// Load the typed config. If it's missing or invalid, create a fresh default and keep a backup of the old file.
    /// </summary>
    public static RoosterAddonConfig LoadOrCreate(IModHelper helper, IMonitor monitor)
    {
        var path = Path.Combine(helper.DirectoryPath, "config.json");

        if (!File.Exists(path))
        {
            var created = Normalize(RoosterAddonConfig.CreateDefault());
            helper.WriteConfig(created);
            return created;
        }

        try
        {
            var cfg = helper.ReadConfig<RoosterAddonConfig>();
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

            monitor.Log(
                $"RoosterAddon: failed to read config.json, writing defaults. Backup: {backupPath}
{ex}",
                LogLevel.Error
            );

            var created = Normalize(RoosterAddonConfig.CreateDefault());
            helper.WriteConfig(created);
            return created;
        }
    }

    public static void Save(IModHelper helper, RoosterAddonConfig cfg)
        => helper.WriteConfig(Normalize(cfg));

    private static RoosterAddonConfig HandleSchemaMismatch(
        IModHelper helper,
        IMonitor monitor,
        string configPath,
        RoosterAddonConfig cfg,
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
            monitor.Log(
                $"RoosterAddon: upgrading config schema {cfg.SchemaVersion} -> {ConfigSchema.CurrentVersion}. Backup: {backupPath}",
                LogLevel.Info
            );

            cfg.SchemaVersion = ConfigSchema.CurrentVersion;
            cfg = Normalize(cfg);
            helper.WriteConfig(cfg);
            return cfg;
        }

        monitor.Log(
            $"RoosterAddon: config schema version {cfg.SchemaVersion} is newer than supported {ConfigSchema.CurrentVersion}. Backup: {backupPath}",
            LogLevel.Warn
        );

        return cfg;
    }

    private static RoosterAddonConfig Normalize(RoosterAddonConfig cfg)
    {
        cfg ??= RoosterAddonConfig.CreateDefault();

        cfg.General ??= new GeneralSettings();
        cfg.Roosters ??= new System.Collections.Generic.Dictionary<RoosterType, RoosterSettings>();
        cfg.FemalesOptions ??= new System.Collections.Generic.Dictionary<string, FemaleRoosterAssignment>();

        foreach (var (key, def) in RoosterAddonConfig.CreateDefaultRoosters())
            cfg.Roosters.TryAdd(key, def);

        return cfg;
    }
}
