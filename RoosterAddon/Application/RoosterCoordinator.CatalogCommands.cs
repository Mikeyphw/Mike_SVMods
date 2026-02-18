using System;
using System.Linq;
using RoosterAddon.Application.Catalogs;
using RoosterAddon.Framework.Config;
using StardewModdingAPI;

namespace RoosterAddon.Application;

internal sealed partial class RoosterCoordinator
{
    private void CatalogCommand(string command, string[] args)
    {
        var sub = args.Length > 0 ? args[0].Trim().ToLowerInvariant() : "help";

        switch (sub)
        {
            case "help":
            case "?":
                Monitor.Log("ra_catalog help", LogLevel.Info);
                Monitor.Log("  ra_catalog reload        - reload catalogs from data/catalogs and apply per CatalogApplyMode", LogLevel.Info);
                Monitor.Log("  ra_catalog export        - export effective catalogs from current config into data/catalogs", LogLevel.Info);
                Monitor.Log("  ra_catalog mode          - show current CatalogApplyMode", LogLevel.Info);
                Monitor.Log("  ra_catalog mode <value>  - set CatalogApplyMode (SeedOnly, RepairInvalid, MergeMissing, Authoritative)", LogLevel.Info);
                break;

            case "reload":
                ReloadCatalogs("console reload");
                break;

            case "export":
                ExportCatalogs("console export");
                break;

            case "mode":
                HandleCatalogMode(args.Skip(1).ToArray());
                break;

            default:
                Monitor.Log($"Unknown ra_catalog subcommand '{sub}'. Use: ra_catalog help", LogLevel.Warn);
                break;
        }
    }

    private void HandleCatalogMode(string[] args)
    {
        if (args.Length == 0)
        {
            Monitor.Log($"CatalogApplyMode = {Config.General.CatalogApplyMode}", LogLevel.Info);
            return;
        }

        if (!Enum.TryParse<CatalogApplyMode>(args[0], ignoreCase: true, out var mode))
        {
            Monitor.Log($"Invalid CatalogApplyMode '{args[0]}'. Valid: {string.Join(", ", Enum.GetNames(typeof(CatalogApplyMode)))}", LogLevel.Error);
            return;
        }

        Config.General.CatalogApplyMode = mode;
        ConfigManager.Save(Helper, Config);
        Monitor.Log($"CatalogApplyMode set to {mode}.", LogLevel.Info);
    }

    private void ReloadCatalogs(string reason)
    {
        _catalogs = _catalogManager.LoadOrCreateAll(Helper, Monitor);

        var changedConfig = _catalogManager.ApplyToConfig(Config, _catalogs, isNewConfig: false, Monitor);
        if (changedConfig)
            ConfigManager.Save(Helper, Config);

        RoosterHelper = new RoosterAddon.Helpers.RoosterControl(Config, Helper, Monitor, _catalogs.Roosters);
        InvalidateDailyPlanCache($"catalog reload: {reason}");
        _needsRoosterSetup = true;

        RequestInvalidate(FarmAnimalsAssetName);
        RequestInvalidate(ObjectsAssetName);

        Monitor.Log($"Catalogs reloaded ({reason}).", LogLevel.Info);
    }

    private void ExportCatalogs(string reason)
    {
        var effective = _catalogManager.CreateFromConfig(Config, _catalogs);
        _catalogManager.WriteAll(Helper, Monitor, effective);

        _catalogs = effective;
        RoosterHelper = new RoosterAddon.Helpers.RoosterControl(Config, Helper, Monitor, _catalogs.Roosters);
        InvalidateDailyPlanCache($"catalog export: {reason}");
        _needsRoosterSetup = true;

        RequestInvalidate(FarmAnimalsAssetName);
        RequestInvalidate(ObjectsAssetName);

        Monitor.Log($"Catalogs exported ({reason}).", LogLevel.Info);
    }
}
