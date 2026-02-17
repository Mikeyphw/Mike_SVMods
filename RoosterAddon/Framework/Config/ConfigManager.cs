using System;
using System.IO;
using System.Text.Json;
using StardewModdingAPI;
using RoosterAddon.Framework;

namespace RoosterAddon.Framework.Config;

/// <summary>Loads, migrates, and saves RoosterAddon config.</summary>
public static class ConfigManager
{
    public static RoosterAddonConfig LoadOrMigrate(IModHelper helper, IMonitor monitor)
    {
        var path = Path.Combine(helper.DirectoryPath, "config.json");
        if (!File.Exists(path))
        {
            var created = RoosterAddonConfig.CreateDefault();
            helper.WriteConfig(created);
            return created;
        }

        if (LooksLikeTypedConfig(path))
        {
            var cfg = helper.ReadConfig<RoosterAddonConfig>();
            if (cfg.SchemaVersion != ConfigSchema.CurrentVersion)
                cfg = MigrateTyped(cfg, monitor);

            return cfg;
        }

        var legacy = helper.ReadConfig<ModConfig>();
        var migrated = LegacyConfigAdapter.FromLegacy(legacy);
        helper.WriteConfig(migrated);

        monitor.Log("RoosterAddon: migrated legacy config.json to typed schema (Phase 3).", LogLevel.Info);
        return migrated;
    }

    public static void Save(IModHelper helper, RoosterAddonConfig cfg)
        => helper.WriteConfig(cfg);

    private static bool LooksLikeTypedConfig(string path)
    {
        try
        {
            using var stream = File.OpenRead(path);
            using var doc = JsonDocument.Parse(stream);
            return doc.RootElement.TryGetProperty("SchemaVersion", out _);
        }
        catch
        {
            return false;
        }
    }

    private static RoosterAddonConfig MigrateTyped(RoosterAddonConfig cfg, IMonitor monitor)
    {
        // Placeholder for future migrations: v1 -> v2 -> ...
        monitor.Log($"RoosterAddon: config SchemaVersion={cfg.SchemaVersion} is not current; keeping as-is.", LogLevel.Warn);
        cfg.SchemaVersion = ConfigSchema.CurrentVersion;
        return cfg;
    }
}
