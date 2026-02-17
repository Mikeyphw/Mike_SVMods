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

            if (cfg.SchemaVersion <= 0)
                cfg.SchemaVersion = ConfigSchema.CurrentVersion;

            if (cfg.SchemaVersion != ConfigSchema.CurrentVersion)
            {
                monitor.Log(
                    $"RoosterAddon: config schema version {cfg.SchemaVersion} differs from current {ConfigSchema.CurrentVersion}. No migrator is registered.",
                    LogLevel.Warn
                );
            }

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
                $"RoosterAddon: failed to read typed config.json, writing defaults. Backup: {backupPath}
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
